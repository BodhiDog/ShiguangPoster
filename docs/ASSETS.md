# 内置风景素材

三张图片使用 **imagegen 技能的内置 image_gen 工具**生成，未使用 API/CLI 后备模式，未安装依赖。它们是 AI 生成的风景，并非对特定真实地点的摄影记录。

图片保存在项目的 `assets/` 目录，并在构建时嵌入 EXE/SCR。以下记录实际采用的完整提示词。

## 山有回响

文件：`assets/mountain-dawn.png`。尺寸：1672 × 941。

```text
Use case: photorealistic-natural
Asset type: a high resolution landscape photograph background bundled in a serene Windows photo screensaver.
Primary request: A breathtaking yet quiet panoramic view of layered Chinese mountain ridges above a sea of mist just after sunrise, a pale peach horizon, blue green mountains gradually fading into the distance, a few pine-covered rock silhouettes at the lower right, natural atmospheric depth.
Style: premium nature editorial photography, physically believable, fine detail, restrained natural color grading, subtle soft morning light, no fantasy elements.
Composition: widescreen landscape 16:9, wide perspective, panoramic depth, generous quiet darker teal and misty space on the left and bottom for UI text that will be overlaid later by the application. The mountain vista fills the entire frame edge to edge.
Constraints: no text, no letters, no watermarks, no frame, no UI, no logos, no people. Output one photograph.
```

## 听，海的呼吸

文件：`assets/quiet-coast.png`。尺寸：1672 × 941。

```text
Use case: photorealistic-natural
Asset type: bundled widescreen wallpaper for a calm Windows photo screensaver.
Primary request: An aerial oblique photograph of a secluded curving beach with translucent turquoise water, soft foamy waves washing onto pale warm sand, a green forest headland on the right and a distant island fading into haze. Late afternoon sunlight. A serene natural composition, premium travel photography, believable textures, restrained color, fine detail, 16:9 landscape.
Composition: water and open sky occupy the left half, beautiful coastline curves toward the lower right. Edge to edge photograph, enough quiet space for separate UI text.
Constraints: No people, buildings, boats, text, letters, logos, watermarks, borders, or UI.
```

## 风经过的地方

文件：`assets/desert-light.png`。尺寸：1672 × 941。

```text
Use case: photorealistic-natural
Asset type: bundled widescreen wallpaper for a calm Windows photo screensaver.
Primary request: Sweeping minimalist sand dunes of the Sahara at the blue hour before sunrise, sculptural softly curving golden apricot ridges under a pale dusty lavender sky, delicate fine ripples in foreground sand, alternating cool mauve shadows and warm peach sunlight on the ridges. Wide panoramic premium landscape photography, natural realistic colors, fine subtle detail, contemplative stillness. 16:9 landscape.
Composition: rolling dunes across the lower two thirds, distant ridges fading in haze; elegant flowing lines, darker open space lower left for separately rendered clock text.
Constraints: No people, animals, plants, buildings, footprints, text, letters, logos, watermarks, borders, or UI.
```
