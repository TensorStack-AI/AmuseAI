// Copyright (c) Adam Clark. All rights reserved.
// Licensed under the Apache 2.0 License.
using System.ComponentModel.DataAnnotations;

namespace Amuse.App.Common
{
    public enum ModelCategoryType
    {
        [Display(Name = "Diffusion")]
        Diffusion = 0,

        [Display(Name = "ControlNets")]
        ControlNet = 1,

        [Display(Name = "Lora Adapters")]
        LoraAdapter = 2,

        [Display(Name = "Upscalers")]
        Upscale = 10,

        [Display(Name = "Extractors")]
        Extract = 20,

        [Display(Name = "Components")]
        Component = 30,

        [Display(Name = "LLMs")]
        LLM = 40
    }
}
