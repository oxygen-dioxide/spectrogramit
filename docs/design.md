# SpectrogramIt — 设计文档

跨平台（桌面 + 移动）语音分析软件。用 C#/.NET MAUI 开发。

## 1. 目标与用户

- **目标用户**：具有一定语音学基础的配音学习者，用于探索自己的语音。
- **场景**：工作间隙在手机上使用；回家在电脑上使用。
- **UX 优先考虑移动端**，但代码完全共享（.NET MAUI，一套 C# 跨 Windows / macOS / iOS / Android）。

## 2. 功能范围

- **音频来源**：打开本地音频文件 + 麦克风录音。
- **无需实时分析**：先录（或加载）→ 再分析。
- **可视化**：
  - 时域波形
  - 二维频谱图（spectrogram）
  - f0（基频）曲线，叠加在频谱上
  - 共振峰（formant）曲线，叠加在频谱上
- **时均频谱 Spectrum**：选区求平均，X=频率（Hz），Y=强度（dB）。

## 3. 架构决策

### 3.1 工程结构（Q7, Q26）

```
SpectrogramIt.Core      — 类库：DSP、分析服务、数据模型、手势逻辑（无 UI 依赖，可单测）
SpectrogramIt           — MAUI App：Shell 导航、SkiaSharp 可视化、各页面
SpectrogramIt.Core.Tests — xUnit 测试（YIN / Burg / 重采样）
```

### 3.2 依赖策略（Q4, Q8, Q19）

- **.NET 10**（环境已装 .NET 10 MAUI workload；原定 .NET 8 因环境原因改用 .NET 10）。
- **NWaves**：纯 C# DSP 基础（时域信号、FFT 等）。
- **共振峰**：Burg LPC **手写移植自 Praat**，多项式求根用 **MathNet.Numerics**。
- **f0**：CREPE 模型（ONNX，**tiny 版 1.87MB**，打包为嵌入资源）+ 移植 **YIN** 作为无模型兜底。
  - 注：rmvpe onnx 约 300MB，放弃；改用 CREPE tiny，实现方式参考 OpenUtau 的 `Crepe.cs`。
- **ONNX 推理**：`Microsoft.ML.OnnxRuntime`（各平台有 native 绑定，OpenUtau 已证实）。
- **可视化渲染**：SkiaSharp（`SkiaSharp.Views.Maui`），波形/频谱/曲线均自绘。

### 3.3 采样率策略（Q5, Q11）

- 分析统一重采样到 **44.1kHz** 做 formant 与 spectrogram。
- CREPE 内部按需到 **16kHz**（模型固定输入采样率）。
- **Core 提供 `AudioResampler` 服务**：各分析器声明所需采样率，Core 按需重采样并缓存（避免重复算）。

### 3.4 可视化渲染（Q6, Q8）

参考 OpenUtau 的 `OtoPlot.cs`（已验证的模式）：
- **频谱图预渲染**为位图/贴图，缩放/平移时通过源矩形切片（source-rect slicing），不逐帧重渲染。
- **波形 / f0 / formant 曲线**作为可缩放矢量叠加在频谱之上。
- 视图状态模型（`xStart` / `xSpan` / center-zoom）直接复用 OtoPlot 的数学。

## 4. 数据模型与分析

### 4.1 频率轴（Q10）

- 可切换 **线性 / 对数（mel）** 轴。
- 查看器页面内提供切换入口。
- 理由：对数对应音乐音符，线性便于看清语音中高次泛音。

### 4.2 无值表示（Q12）

- f0 / 共振峰用 **0 表示无值**（unvoiced 区段）。
- 图上断线表示；不做 unvoiced 背景遮罩（依选择，用 0 + 断线）。

### 4.3 共振峰移植（Q13, Q14）

- **算法链路**：Burg LPC（手写，移植自 Praat `NUM2.cpp:1430` 的 `VECburg`）→ 多项式（伴随矩阵）求根（MathNet.Numerics `ComplexEigenDecomposition`）→ 根对 → 频率 `atan2(imag,real)·nyquist/π`、带宽 `log|root|`。
- 参照 Praat `Sound_to_Formant.cpp`。
- **可调参数**：共振峰条数（默认 4）、最大分析频率（默认 5kHz）、窗长/时步**三档预设**（fast / default / precise）。

### 4.4 f0（Q15）

- **CREPE（ONNX）为主**（精度），**YIN 为兜底**（无模型依赖、可交叉验证）。
- 都能区分 voiced / unvoiced（用 0 表示无值）。

### 4.5 数据/文件模型（Q9）

- 录制音频保存到本地默认录音文件夹（时间戳 wav 文件名）。
- 分析结果**不落盘**，无"工程"概念。本质 = 录音机 + 音频查看器。

## 5. 录音与播放

### 5.1 录音流程（Q16）

- 录音时**内存缓冲 + 实时画简单波形预览**（非分析）。
- 停止 → 自动存到默认录音文件夹 → 自动进入查看器加载该文件。

### 5.2 播放（Q17）

- 支持播放音频。
- 波形上画**播放头游标**，边放边移。
- **点击波形定位**。
- （选区 A/B 循环播放见下节；A/B 循环作为后续增强。）

## 6. 选区（A/B）与时均频谱

### 6.1 选区语义（Q20, Q21）

- 选区 = **A/B 播放区间**：选中单个音符认真听并分析其唱法。
- 选区内：**平均 f0、平均共振峰数值**随选区变化**实时显示**。
- 频谱页/面板 **实时重算**（反映当前选区）。

### 6.2 手势状态机（Q21）

封装为 `SelectionGestureHandler`（两端复用）：

**桌面端**
- 左键拖曳 = 选区
- 平移 = 滚轮（横向）或 **空格 + 左键拖**
- 缩放 = **Ctrl + 滚轮**（光标为中心），UI 提供放大/缩小按钮
- 双击 = 清除选区

**移动端**
- 单指拖 = 平移
- 双指捏合 = 缩放
- **长按 = 进入选区模式**（提示"选区模式"，点 × 或再次长按退出），随后单指拖 = 扩展选区

### 6.3 频谱显示布局（Q23, Q24）

- **自适应布局，按屏幕尺寸而非按 OS 一刀切**（平板也可享受桌面式布局）：
  - **大屏（桌面/平板）**：同页**右侧可折叠面板**，选区变化实时重算频谱。
  - **小屏（手机）**：点"平均频谱"进入**全屏频谱页面**，点 × 返回。
- 两端共用同一个 `AverageSpectrumViewModel`，只是承载载体不同。
- **移动端流程**：进频谱页前先选好区；频谱页**只读**显示当前选区结果；想看别的区 → 返回 → 重选 → 再进。

## 7. 模型文件打包（Q25）

- CREPE tiny ONNX（1.87MB）作为**嵌入资源**打进 Core 程序集，开箱即用，无需网络。
- 实现方式参考 OpenUtau 的 `Crepe.cs`（`Resources.tiny` 模式）。

## 8. 页面结构（补充）

软件包含以下页面（Shell 导航）：

- **欢迎页**：显示**最近打开的文件**列表（含最近录音），点击进入音频查看页。
- **音频查看页**：核心可视化页面（波形 + 频谱 + f0 + 共振峰 + 选区 + 时均频谱）。
- **设置页**：共振峰参数（条数、最大频率、窗长/时步三档）、频率轴（线性/对数）、录音默认文件夹等。

## 9. 里程碑（Q27）

### 一期（MVP）
- 只做：**打开 wav → 频谱图 + f0（YIN）曲线**。
- 验证渲染管线与手势状态机。

### 二期
- 录音
- CREPE f0（替换/补充 YIN）
- 共振峰 formant（Burg + MathNet）
- 选区 A/B + 时均频谱
- 多平台打磨（自适应布局、动画、性能）

## 9. 参考实现

- OpenUtau：`OpenUtau.Core/Analysis/Crepe/Crepe.cs`、`OpenUtau/Controls/OtoPlot.cs`、`OpenUtau/Controls/WaveformImage.cs`
- Praat：`fon/Sound_to_Formant.cpp`、`dwsys/NUM2.cpp`（VECburg）、`dwsys/Roots.cpp`（Polynomial_to_Roots）