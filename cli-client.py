#!/usr/bin/env python3
"""Spirit Merge CLI — Unity Editor TCP 클라이언트
사용법:
  python client.py ping
  python client.py errors
  python client.py exec SpiritMerge.Editor.SpiritDataGenerator.CreateAllSpiritData
  python client.py refresh
  python client.py build-webgl
"""

import socket
import sys
import time

# Windows 콘솔(cp949)에서 이모지/한글 출력 깨짐 방지 — UTF-8 강제
try:
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    sys.stderr.reconfigure(encoding='utf-8', errors='replace')
except Exception:
    pass

HOST = '127.0.0.1'
PORT = 5555
TIMEOUT = 10

def send_command(cmd: str) -> str:
    """TCP로 명령을 보내고 응답을 받습니다. (응답이 커도 전체 수신)"""
    try:
        with socket.create_connection((HOST, PORT), timeout=TIMEOUT) as sock:
            sock.sendall((cmd + '\n').encode('utf-8'))
            data = b''
            while True:
                try:
                    chunk = sock.recv(65536)
                except socket.timeout:
                    break  # 서버가 연결 유지 중이면 타임아웃까지 수신
                if not chunk:
                    break  # 서버가 연결 닫음
                data += chunk
                if len(data) > 5_000_000:
                    break
            return data.decode('utf-8').strip()
    except ConnectionRefusedError:
        return "❌ Unity Editor가 실행 중인가요? (또는 CliServer가 로드되지 않음)"
    except socket.timeout:
        return "⏱ 응답 시간 초과"
    except Exception as e:
        return f"⚠️ 오류: {e}"


def local_errors():
    """Unity 서버 없이 Editor.log를 직접 읽어 컴파일 에러 검색.
    세이프티 모드/에디터 미로드 상태에서도 동작 (CliServer 불필요)."""
    import os
    candidates = []
    proj_log = os.path.join(os.getcwd(), "Logs", "Editor.log")
    if os.path.exists(proj_log):
        candidates.append(proj_log)
    global_log = os.path.join(os.environ.get("LOCALAPPDATA", ""), "Unity", "Editor", "Editor.log")
    if global_log and os.path.exists(global_log):
        candidates.append(global_log)
    if not candidates:
        return "⚠️ Editor.log 없음 (Unity를 한 번은 실행해야 로그 생성됨)"

    errors = []
    newest = 0
    for path in candidates:
        try:
            mt = os.path.getmtime(path)
            if mt > newest:
                newest = mt
            with open(path, "r", encoding="utf-8", errors="replace") as f:
                content = f.read()  # ⭐ 전체 읽기 (앞쪽 에러도 놓치지 않도록)
            lines = content.splitlines()

            # ⭐ 마지막 컴파일 세션 이후의 에러만 — 이전 컴파일 실패 에러 잔재 제외
            #    Unity 로그: "[ScriptCompilation] Requested script compilation" → 에러 → "Reloading assemblies..."
            start = 0
            for i, l in enumerate(lines):
                if "ScriptCompilation" in l or "Compilation" in l:
                    start = i

            for line in lines[start:]:
                # ⭐ Windows 경로(백슬래시)와 정규 경로(슬래시) 모두 매칭
                if "error CS" in line and ("Assets/Scripts" in line or "Assets\\Scripts" in line):
                    line = line.strip()
                    if line not in errors:
                        errors.append(line)
        except Exception as e:
            return f"⚠️ 로그 읽기 실패 ({path}): {e}"

    if not errors:
        return "ok: no errors"
    return f"❌ 컴파일 에러 {len(errors)}개:\n" + "\n".join(errors[:30])


def main():
    if len(sys.argv) < 2:
        print("사용법: python client.py <명령어> [인자]")
        print("")
        print("명령어:")
        print("  ping                    연결 확인")
        print("  play                    Play Mode 진입")
        print("  stop                    Play Mode 종료")
        print("  errors                  컴파일 에러 확인 (로컬 Editor.log 검색 — 서버 불필요)")
        print("  exec <Class.Method>     메서드 실행")
        print("  refresh                 AssetDatabase 리프레시")
        print("  build-webgl             WebGL 빌드")
        print("  restart                 CliServer 재시작")
        print("  quit                    서버 종료")
        print("  tags                    game_log 카테고리 목록")
        print("  cat <카테고리> [줄수]     특정 카테고리 로그 마지막 N줄 (기본 40)")
        print("                          예: cat system 30, cat errors 100")
        print("  fulltest                전체 시스템 통합 검증 실행 (CmdFullTest)")
        return

    cmd = sys.argv[1]

    # ⭐ errors는 Unity 서버 없이 로컬 Editor.log로 즉시 확인 (세이프티 모드 대응)
    if cmd == "errors":
        print(local_errors())
        return

    if cmd == "exec" and len(sys.argv) >= 3:
        full_cmd = f"method:{sys.argv[2]}"
    elif cmd == "cat" and len(sys.argv) >= 3:
        # cat <카테고리> [줄수] — 인자를 하나의 명령으로 조합
        full_cmd = "cat " + " ".join(sys.argv[2:])
    elif cmd == "fulltest":
        print("🔄 전체 시스템 통합 검증 실행 (CliTestSuite.CmdFullTest) ...")
        print(send_command("method:SpiritMerge.Cli.CliTestSuite.CmdFullTest"))
        print("")
        print("--- [FullTest] 결과 로그 ---")
        time.sleep(2)
        seen = set()
        for category in ("cli", "misc"):
            log = send_command(f"cat {category} 60")
            for line in log.splitlines():
                if "[FullTest]" in line and line not in seen:
                    seen.add(line)
                    print(line)
        return
    else:
        full_cmd = cmd

    response = send_command(full_cmd)
    print(response)


if __name__ == "__main__":
    main()
