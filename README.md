# Amuse
Amuse is the flagship demo application for the [TensorStack SDK](https://github.com/saddam213/TensorStack), showcasing high-performance local AI image, video, audio and text generation through a modern, extensible .NET architecture.

<div align="center">
   <h1><a href="https://github.com/saddam213/AmuseAI/releases/download/v3.7.3/Amuse_v3.7.3.exe">Download Amuse v3.7.3</a></h1>
</div>

## Features
* Automatic installation of an isolated, Python environment.
* Safetensors, GGUF, and ONNX support.
* Video Editor for generated or local content.
* Image/Video Upscale for static and moving media.
* Feature Extraction from images and video.
* Video Interpolation for frame rates and slow-motion.
* Image Inpaint to remove objects or fill areas.
* Advanced Image Editing with selection and masking tools.
* Voice Generation (Supertonic).
* Speech Recognition (Whisper).
* Media Gallery for organization and management.
* Lora/ControlNet Support for output control.

---

## Image Pipelines
- Z-Image
- Qwen
- FLUX.1
- FLUX.2
- Chroma
- Kandinsky5
- StableDiffusion-XL
- StableDiffusion-3
- Ernie Image
- Anima
- JoyAI Image
- PRX-Pixel
- Krea2
- GLM Image
- Ideogram 4

## Video Pipelines
- LTX
- LTX-2
- Wan 2.2
- CogVideoX
- Kandinsky5
- SkyReels-V2
- Helios
- Motif
- AnyFlow
- MiniMax-H3

## Audio Pipelines
- ACE-Step
- ACE-Step XL
- Whisper
- Supertonic v3
- LongCat Audio

## Text Pipelines
- Qwen 3.0
- Qwen 3.5
- Qwen 3.6
- Qwen 3.8
- Gemma 4.0
---

## GPU Support
Amuse leverages `CUDA 13.0`, providing native support for the latest generation of hardware.<br /> While legacy architectures (Pascal/Maxwell) are technically supported, an RTX-enabled card is strongly recommended to utilize Tensor Cores for efficient generation speeds.
<table>
  <thead>
    <tr>
      <th>Architecture</th>
      <th>Platform Support</th>
      <th>GPU Models</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td><b>Blackwell</b><br/>(SM_100)</td>
      <td>Windows 10<br/>Windows 11*<br/>Server 2022</td>
      <td>GeForce RTX 5090, 5080, 5070 Ti, 5070;<br/>RTX PRO Blackwell series</td>
    </tr>
    <tr>
      <td><b>Ada Lovelace</b><br/>(SM_89)</td>
      <td>Windows 10<br/>Windows 11*<br/>Server 2022</td>
      <td>GeForce RTX 4090, 4080, 4070 Ti/Super, 4070, 4060 Ti, 4060;<br/>RTX 6000/5000/4000 Ada</td>
    </tr>
    <tr>
      <td><b>Ampere</b><br/>(SM_86)</td>
      <td>Windows 10<br/>Windows 11*<br/>Server 2022</td>
      <td>GeForce RTX 3090 Ti, 3090, 3080 Ti, 3080, 3070 Ti, 3070, 3060 Ti, 3060;<br/>RTX A-series (A6000, etc.)</td>
    </tr>
    <tr>
      <td><b>Turing</b><br/>(SM_75)</td>
      <td>Windows 10<br/>Windows 11*<br/>Server 2022</td>
      <td>GeForce RTX 2080 Ti, 2080 Super, 2070, 2060;<br/>GTX 1660 Ti, 1660 Super, 1650</td>
    </tr>
  </tbody>
</table>

> Note: Minimum Driver (NVIDIA): `Version 580.65` or later is required for `CUDA 13.0` compatibility.

> ** Windows 11 users may need to `Run As Administrator` during environment creation if there are failures.

---

<div align="center">
   <h1><a href="https://github.com/saddam213/AmuseAI/releases/download/v3.7.3/Amuse_v3.7.3.exe">Download Amuse v3.7.3</a></h1>
</div>


### External Dependencies
- `PdfPig` https://github.com/UglyToad/PdfPig
- `Markdig` https://github.com/xoofx/markdig
- `Serilog` https://github.com/serilog/serilog
- `ColorCode` https://github.com/CommunityToolkit/ColorCode-Universal
- `TensorStack` https://github.com/saddam213/TensorStack
- `Diffusers` https://github.com/huggingface/diffusers
- `Transformers` https://github.com/huggingface/transformers
- `StableDiffusion.cpp` https://github.com/leejet/stable-diffusion.cpp