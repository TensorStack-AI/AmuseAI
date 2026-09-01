// Copyright (c) Adam Clark. All rights reserved.
// Licensed under the Apache 2.0 License.
using Amuse.App.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using TensorStack.WPF.Controls;

namespace Amuse.App.Dialogs
{
    /// <summary>
    /// Interaction logic for ModelInformationDialog.xaml
    /// </summary>
    public partial class ModelInformationDialog : DialogControl
    {
        private IDownloadModel _downloadModel;

        public ModelInformationDialog(Settings settings)
        {
            Settings = settings;
            ComponentMap = Settings.Components.ToDictionary(k => k.Key, v => v.Checkpoint?.DownloadFiles);
            InitializeComponent();
        }

        public Settings Settings { get; }
        public Dictionary<string, string[]> ComponentMap { get; }

        public IDownloadModel DownloadModel
        {
            get { return _downloadModel; }
            set { SetProperty(ref _downloadModel, value); }
        }


        public Task<bool> ShowDialogAsync(IDownloadModel downloadModel)
        {
            DownloadModel = downloadModel;
            return base.ShowDialogAsync();
        }
    }



    public class DictionaryLookupMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is string path && values[1] is IDictionary<string, string[]> dictionary && dictionary.TryGetValue(path, out var componentFiles))
            {
                return componentFiles;
            }
            return values[0];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
