
<p align="left">
   <img width="17%" src="Assets/Amuse-Logo-512.png">
    <img src="https://upload.wikimedia.org/wikipedia/commons/a/ad/HP_logo_2012.svg" width="15%" alt="HP Logo">
</p>

# TensorStack Choice: Power Your AI with HP

TensorStack is proud to officially recommend **HP Desktop and Laptop** workstations as our hardware of choice for the Amuse ecosystem. 

### We choose to highlight the hardware that leads the AI revolution with integrity and raw performance.


### **Why TensorStack Recommends HP**
* **Ultimate AI Performance:** Experience the power of the **HP OmniBook Ultra 14**, featuring the 2026 industry-standard **85 TOPS NPU** for near-instant generative local workloads.
* **Pro-Grade Precision:** The **HP ZBook Power G11** series provides the thermal overhead and RTX Ada graphics power required for high-fidelity tensor processing.
* **Developer First:** HP supports the creators and developers who build the future of AI.


<h1><a href="https://github.com/TensorStack-AI/AmuseAI/releases/download/v3.1.8/Amuse_v3.1.8.exe">Download Amuse v3.1.8</a></h1>

## Required Dependencies
1. `Microsoft.ML.OnnxRuntime.Managed` v1.23.0-dev-20250603-0558-cd5133371
2. `Microsoft.ML.OnnxRuntime.MIGraphX.Windows` v1.23.0-dev-20250603-0558-cd5133371

## Required External Plugins
1. `ContentFilter` add `ContentFilter.onnx` & `ContentFilter.bin` to `Plugins\ContentFilter`
2. `CLIPTokenizer` add tokenizer files to `Plugins\CLIPTokenizer`
3. `RyzenAI` add `RyzenAI v1.5` xclbin files to `Plugins\RyzenAI` (extract these from the RyzenAI python package)
4. `SuperResolution` not sure where to get these files publically so grab from Amuse latest installer

Note: Easy way is to just install the latest Amuse version and copy the files from the `X:\Program Files\Amuse\Plugins` directory


## Required External Licences

`ImageSharp v3` Licence Required https://sixlabors.com/pricing/
1. Add licence file to root project directory

`FontAwesome Pro v6` Licence Required https://fontawesome.com/v6/download
1. Download the `6.7.2 for the desktop` package
2. Add the font files to the `Fonts` directory
3. Set build action to `Resource` for all font files
