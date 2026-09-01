// Copyright (c) Adam Clark. All rights reserved.
// Licensed under the Apache 2.0 License.
using Amuse.Common;
using TensorStack.WPF.Controls;

namespace Amuse.App.Common
{
    public record ModelViewOpenArgs(ModelCategoryType ModelType, PipelineType? PipelineType = null) : OpenViewArgs;
}
