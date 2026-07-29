# Spirit Merge — AI 이미지 생성 가이드 (ComfyUI)

## 🎨 통일 스타일 가이드

| 항목 | 값 |
|------|-----|
| 스타일 | **Stylized 2D RPG** (SD 스타일, 셀풍 + 약간의 페이트 텍스처) |
| 팔레트 | 어두운 배경(남색/흑색) + **속성별 포인트 칼라** |
| 배경 | 투명 (PNG, alpha channel) |
| 해상도 | **512×512** (1:1 정사각형) |
| 파일명 규칙 | 영어 소문자, 띄어쓰기 = 언더바 |
| 저장 위치 | `Assets/Sprites/` |

---

## 1. 속성 아이콘 (Element Icons)

**용도:** GNB 탭 아이콘, 정령 슬롯 속성 표시, 전투 UI

### 공통 조건

```
Simple game icon, minimalist, flat design, 
centered on dark transparent background, 
rounded square border, glowing edge,
32x32 game icon style, pixel-perfect,
PNG with alpha channel, game asset icon
```

### 개별 속성 (4종)

| 파일명 | 속성 | 추가 키워드 |
|--------|------|-----------|
| `icon_fire.png` | 🔥 불 | **orange-red flame** symbol, fire icon, bright core, ember particles |
| `icon_water.png` | 💧 물 | **deep blue water droplet** symbol, water icon, ripple effect, aqua glow |
| `icon_nature.png` | 🌿 자연 | **emerald green leaf** symbol, nature icon, vine tendrils, soft glow |
| `icon_thunder.png` | ⚡ 번개 | **gold-yellow lightning bolt** symbol, thunder icon, electric sparks, sharp glow |

---

## 2. 적 몬스터 (Enemy Sprites)

**용도:** 전투 필드의 적 유닛

### 공통 조건

```
2D game character sprite, full body, front facing,
stylized RPG monster, chibi proportion (head:body = 1:1.5),
clean silhouette, readable at 64x64,
dark transparent background, game asset,
no background, PNG alpha
```

### 챕터별 적 목록 (5챕터 × 3종 = 15종)

#### 챕터 1: 🔥 불의 숲 (Fire)

| 파일명 | 이름 | 설명 | 추가 키워드 |
|--------|------|------|-----------|
| `enemy_ch1_01.png` | 불꽃 슬라임 | 작은 빨간 슬라임, 불꽃 같은 표면 | red fire slime, molten surface, small cute eyes, ember spots |
| `enemy_ch1_02.png` | 불 도마뱀 | 붉은 도마뱀, 꼬리에 불꽃 | red lizard, flame tail, orange scales, sharp teeth |
| `enemy_ch1_03.png` | 불 정령 (보스) | 큰 불 정령, 불꽃 갈기 | large fire spirit, mane of flames, intense yellow eyes, floating |

#### 챕터 2: 💧 물의 숲 (Water)

| 파일명 | 이름 | 설명 | 추가 키워드 |
|--------|------|------|-----------|
| `enemy_ch2_01.png` | 물 슬라임 | 파란 물방울 슬라임 | blue water slime, translucent body, aqua tint, tiny fins |
| `enemy_ch2_02.png` | 물고기병 | 푸른 비늘 인간형 물고기 | blue scaled fish soldier, spear, fin crest, scales |
| `enemy_ch2_03.png` | 물 정령 (보스) | 큰 물 정령, 해초 갈기 | large water spirit, seaweed tendrils, glowing blue core, floating |

#### 챕터 3: 🌿 숲의 길 (Nature)

| 파일명 | 이름 | 설명 | 추가 키워드 |
|--------|------|------|-----------|
| `enemy_ch3_01.png` | 덩굴 슬라임 | 초록 덩굴 슬라임 | green vine slime, leaf sprouts, moss texture, small flower |
| `enemy_ch3_02.png` | 나무 병사 | 나무로 된 인간형 병사 | wooden soldier, bark armor, branch arms, leaf cloak, moss |
| `enemy_ch3_03.png` | 자연 정령 (보스) | 큰 나무 정령, 꽃 왕관 | large nature spirit, flower crown, glowing green, root tendrils |

#### 챕터 4: ⚡ 번개의 숲 (Thunder)

| 파일명 | 이름 | 설명 | 추가 키워드 |
|--------|------|------|-----------|
| `enemy_ch4_01.png` | 번개 슬라임 | 노란 번개 슬라임 | yellow thunder slime, static sparks, jagged surface, electric |
| `enemy_ch4_02.png` | 구름 병사 | 작은 구름 형태 병사 | small cloud soldier, lightning bolts, stormy, floating, zap |
| `enemy_ch4_03.png` | 번개 정령 (보스) | 큰 번개 정령, 전기 오라 | large thunder spirit, electric aura, crackling energy, bright core |

#### 챕터 5: 🔥 불의 성소 (Fire — 고급)

| 파일명 | 이름 | 설명 | 추가 키워드 |
|--------|------|------|-----------|
| `enemy_ch5_01.png` | 마그마 골렘 | 바위+용암 골렘 | magma golem, cracked rock body, lava veins, strong arms |
| `enemy_ch5_02.png` | 불사조 (중간) | 작은 불새 | small phoenix, flame wings, golden glow, majestic, flying |
| `enemy_ch5_03.png` | 혼돈의 정령 (최종 보스) | 보라+빨강 혼합 정령 | chaos spirit, purple-red mix, void eyes, dark flames, massive |

---

## 3. 생성 팁

### ComfyUI 설정 추천

```
Checkpoint:  any anime/2D model (e.g. anything-v5, counterfeit, etc.)
Sampler:     DPM++ 2M Karras
Steps:      25-30
CFG:        7
Size:       512×512
Batch:      4 (한 번에 4개 생성 → 선택)
```

### 배경 제거

- ComfyUI에서 **RemBG** 노드 사용
- 또는 생성 후 `remove.bg` 등으로 후처리
- 최종적으로 **PNG (투명 배경)** 으로 저장

### 이미지 후보정

```
1. 생성된 이미지 중 가장 좋은 것 선택
2. 필요시 Photoshop/GIMP로 간단히 정리
3. 64×64 정도로 축소해도 식별 가능해야 함
```

---

완성되면 `Assets/Sprites/` 폴더에 넣어주세요!
그럼 제가 `Connect Spirit Sprites`처럼 적 스프라이트도 자동 연결해드릴게요! 😊
