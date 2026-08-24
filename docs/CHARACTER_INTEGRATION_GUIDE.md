# Character Integration Guide

이 문서는 최종 Humanoid 모델을 처음 Unity에 넣는 작업자를 위한 절차다. 현재 프로젝트에는 최종 모델/Avatar/animation clip이 없으며 procedural placeholder가 정상 fallback이다.

## 1. FBX 넣기

1. Unity 아래 `Project` 창에서 `Assets > _Game > Art > Characters`를 연다.
2. 빈 곳에서 마우스 오른쪽 버튼을 누르고 `Create > Folder`를 선택해 캐릭터 이름 폴더를 만든다.
3. Windows 탐색기에서 FBX와 texture를 이 폴더로 드래그한다.
4. import가 끝날 때까지 Unity 오른쪽 아래 progress가 사라질 때까지 기다린다.

## 2. Humanoid Avatar 만들기

1. Project 창에서 FBX를 한 번 클릭한다.
2. 오른쪽 `Inspector` 위쪽의 `Rig` 탭을 클릭한다.
3. `Animation Type` dropdown을 `Humanoid`로 바꾼다.
4. `Avatar Definition`을 `Create From This Model`로 둔다.
5. Inspector 오른쪽 아래 `Apply`를 클릭한다.
6. `Configure...` 버튼을 클릭한다.
7. Head, Spine/Chest, Hips, 양쪽 Arm/Hand, Leg/Foot slot이 초록색인지 확인한다.
8. 빨간 slot이 있으면 Hierarchy의 올바른 bone을 해당 slot으로 드래그한다. vendor bone 이름을 코드에 적지 않는다.
9. 아래 `Pose` menu에서 필요 시 `Enforce T-Pose`를 누르고 `Apply`, `Done`을 차례로 누른다.

Avatar가 invalid이거나 Generic이면 SwingPop validator가 Humanoid Animator를 승인하지 않고 procedural fallback을 유지한다.

## 3. Integration prefab 만들기

1. `Assets > _Game > Prefabs > Characters`를 연다.
2. `HumanoidGolferTemplate`를 선택하고 `Ctrl+D`로 복제한다.
3. 복제본 이름을 캐릭터 이름으로 바꾼다.
4. 복제본을 더블클릭해 Prefab Mode를 연다.
5. FBX model prefab을 `VisualRoot` 아래로 드래그한다.
6. model의 Transform Position은 `(0,0,0)`, Rotation은 `(0,0,0)`, Scale은 우선 `(1,1,1)`로 시작한다.
7. gameplay root의 Transform scale은 바꾸지 않는다. 크기/발 높이는 profile의 `Character Scale`, `Ground Offset`으로 조정한다.

## 4. Adapter reference 연결

1. Prefab Hierarchy에서 최상위 캐릭터 root를 클릭한다.
2. Inspector의 `Character Visual Adapter`를 찾는다.
3. 다음 object를 각 slot에 드래그한다.
   - `Gameplay Root`: 최상위 root
   - `Visual Root`: VisualRoot
   - `Animator`: FBX model의 Animator
   - `Avatar`: FBX에서 생성된 Avatar
   - `Hand Socket`: 기본 hand attachment
   - `Left Hand Socket`: 왼손 marker
   - `Right Hand Socket`: 오른손 marker
   - `Club Socket`: RightHandSocket 아래 ClubSocket
   - `Impact Anchor`: 공 접촉 위치 marker
   - `Head Look Target`: 머리가 바라볼 기준 marker
   - `Profile`: 해당 캐릭터용 CharacterVisualProfile
4. 두 손 grip 보정은 향후 constraint/IK pass에서 한다. 이번 단계에서 IK package나 장비 시스템을 추가하지 않는다.

## 5. CharacterVisualProfile 설정

1. Project 창에서 profile asset을 선택한다.
2. `Visual Height`에 model의 meter 높이를 기록한다.
3. `Local Bounds Center/Size`를 renderer bounds에 맞춘다.
4. 발이 뜨거나 묻히면 `Ground Offset`만 소량 조정한다.
5. 전체 visual 크기는 `Character Scale`로 조정한다.
6. Address pivot 차이는 `Address Offset`, camera 기준은 `Camera Framing Offset`에 기록한다.
7. Club/Impact/Head offset은 socket marker와 같은 기준값으로 기록한다.
8. `Portrait Reference`는 portrait가 준비됐을 때만 연결한다.

이 값은 presentation metadata이며 Ball, Shot, Club power, Camera state를 변경하지 않는다.

## 6. Animator Controller 만들기

1. Project 창 빈 곳에서 마우스 오른쪽 버튼을 누른다.
2. `Create > Animator Controller`를 선택한다.
3. 이름을 예: `MiraAnimatorController`로 바꾼다.
4. 더블클릭해 Animator 창을 연다.
5. clip을 드래그해 다음 이름의 state를 만든다: Idle, Address, BackSwing, Swing, FollowThrough, WatchBall, PuttAddress, PuttBackSwing, PuttSwing, PuttFollowThrough, Happy, Sad, BirdieCelebration, EagleCelebration, HoleInOneCelebration.
6. state 이름은 clip 파일명이 아니라 위 계약과 정확히 일치해야 한다.
7. Controller를 model Animator의 `Controller` slot에 드래그한다.
8. `Apply Root Motion`은 끈다.

`CharacterAnimatorContract`가 state hash를 관리하므로 gameplay code에서 clip 문자열을 추가하지 않는다.

## 7. Impact Animation Event 넣기

1. Prefab Mode에서 Animator가 있는 model object를 선택한다.
2. 상단 menu에서 `Window > Animation > Animation`을 클릭한다.
3. Animation 창 왼쪽 위 clip dropdown에서 `Swing` clip을 선택한다.
4. timeline을 club head가 Ball과 접촉하는 정확한 frame으로 이동한다.
5. Animation 창 위쪽의 `Add Event` 버튼(작은 marker 아이콘)을 클릭한다.
6. 생성된 event marker를 클릭한다.
7. Inspector의 Function dropdown에서 `NotifyImpactAnimationEvent`를 선택한다.
8. Swing clip에는 이 event가 정확히 1개만 있는지 확인한다.
9. `PuttSwing` clip에도 같은 절차로 정확히 1개를 넣는다.

Function이 dropdown에 보이지 않으면 `CharacterAnimationController`가 같은 prefab root에 있는지, 선택한 object가 그 hierarchy 안에 있는지 확인한다. Event가 없어도 normalized fallback이 soft lock을 막지만, 최종 timing 기준은 Animation Event다.

## 8. 자동 검사

1. 상단 menu에서 `SwingPop > Character > Validate Character Setup`을 클릭한다.
2. `PASS` dialog가 뜨면 Animator/Avatar/socket/profile 기준을 통과한 것이다.
3. `missing`, `invalid`, `not Humanoid` 메시지가 나오면 해당 Inspector slot을 먼저 고친다.
4. validator는 bone 이름을 자동 추측하거나 model hierarchy를 수정하지 않는다.

## 9. 상태 미리보기

1. `Assets > _Game > Scenes > Hole01_SkyIsland`를 더블클릭한다.
2. Unity 상단 중앙 `Play` 버튼을 누른다.
3. 상단 menu에서 `SwingPop > Character > Preview Character States`를 클릭한다.
4. Address, Swing, Putt Swing, Birdie Celebration 버튼으로 pose를 확인한다.
5. preview Swing/Putt는 Impact를 비활성화하므로 Ball이나 stroke를 바꾸지 않는다.
6. Play를 끝낼 때 상단 `Play` 버튼을 다시 누른다.

## 10. 최종 회귀 확인

다음 전체 흐름을 일반 입력으로 별도 확인한다.

1. Address에서 Character/Ball/Club/Aim/Fairway가 함께 보인다.
2. Driver shot이 Commit 후 즉시가 아니라 Swing Impact marker에서 한 번만 launch된다.
3. FollowThrough와 WatchBall로 넘어간다.
4. 공이 멈추면 캐릭터가 다음 Ball 위치로 이동하고 올바른 방향을 본다.
5. Green에서 Putter visual과 Putt animation으로 바뀐다.
6. Putt camera에서 Ball과 Cup이 함께 보인다.
7. Hole-In 후 올바른 celebration과 Result camera가 나온다.
8. Console Error 0, Missing Script/Reference 0인지 확인한다.

문제가 생기면 gameplay script를 바꾸기 전에 Adapter reference, Avatar validity, Animator state 이름, Impact Event 개수, profile scale/ground offset을 먼저 확인한다.
