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

HOST = '127.0.0.1'
PORT = 5555
TIMEOUT = 10

def send_command(cmd: str) -> str:
    """TCP로 명령을 보내고 응답을 받습니다."""
    try:
        with socket.create_connection((HOST, PORT), timeout=TIMEOUT) as sock:
            sock.sendall((cmd + '\n').encode('utf-8'))
            response = sock.recv(65536).decode('utf-8')
            return response.strip()
    except ConnectionRefusedError:
        return "❌ Unity Editor가 실행 중인가요? (또는 CliServer가 로드되지 않음)"
    except socket.timeout:
        return "⏱ 응답 시간 초과"
    except Exception as e:
        return f"⚠️ 오류: {e}"


def main():
    if len(sys.argv) < 2:
        print("사용법: python client.py <명령어> [인자]")
        print("")
        print("명령어:")
        print("  ping                    연결 확인")
        print("  errors                 컴파일 에러 확인")
        print("  exec <Class.Method>    메서드 실행")
        print("  refresh                AssetDatabase 리프레시")
        print("  build-webgl            WebGL 빌드")
        print("  quit                   서버 종료")
        return

    cmd = sys.argv[1]
    if cmd == "exec" and len(sys.argv) >= 3:
        full_cmd = f"method:{sys.argv[2]}"
    else:
        full_cmd = cmd

    response = send_command(full_cmd)
    print(response)


if __name__ == "__main__":
    main()
