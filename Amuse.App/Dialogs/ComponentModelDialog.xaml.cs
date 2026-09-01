// Copyright (c) Adam Clark. All rights reserved.
// Licensed under the Apache 2.0 License.
using Amuse.App.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TensorStack.WPF;
using TensorStack.WPF.Controls;
using TensorStack.WPF.Services;

namespace Amuse.App.Dialogs
{
    /// <summary>
    /// Interaction logic for ComponentModelDialog.xaml
    /// </summary>
    public partial class ComponentModelDialog : DialogControl
    {
        private ComponentModel _componentModel;
        private ComponentModel _originalComponentModel;

        public ComponentModelDialog(Settings settings)
        {
            Settings = settings;
            SaveCommand = new AsyncRelayCommand(SaveAsync, CanExecuteSave);
            CancelCommand = new AsyncRelayCommand(CancelAsync);
            Errors = new ObservableCollection<string>();
            InitializeComponent();
        }

        public Settings Settings { get; }
        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand CancelCommand { get; }
        public ObservableCollection<string> Errors { get; }
        public CheckpointType[] CheckpointTypes { get; } = [CheckpointType.OnlineFolder, CheckpointType.LocalFolder];
        public bool IsUpdateMode => _originalComponentModel is not null;

        public ComponentModel ComponentModel
        {
            get { return _componentModel; }
            set { SetProperty(ref _componentModel, value); }
        }


        public Task<bool> AddAsync()
        {
            var modelId = Settings.Components.NextId();
            ComponentModel = new ComponentModel
            {
                Id = modelId,
                Name = "New Component",
                Key = "NEW_COMP",
                Checkpoint = new CheckpointComponent
                {
                    Name = "Component",
                    Folder = "Components",
                    Type = CheckpointType.LocalFolder
                }
            };
            return base.ShowDialogAsync();
        }


        public Task<bool> UpdateAsync(ComponentModel componentModel)
        {
            var modelId = componentModel.Id;
            _originalComponentModel = componentModel;
            ComponentModel = componentModel.DeepClone(modelId);
            return base.ShowDialogAsync();
        }


        public Task<bool> CopyAsync(ComponentModel componentModel)
        {
            var modelId = Settings.Components.NextId();
            ComponentModel = componentModel.DeepClone(modelId);
            ComponentModel.Name += " copy";
            return base.ShowDialogAsync();
        }


        public async Task<bool> ImportAsync(ComponentModel[] modelImports)
        {
            var modelId = Settings.Components.NextId();
            if (modelImports.Length == 1)
            {
                var modelImport = modelImports[0];
                modelImport.Id = modelId;
                ComponentModel = modelImport;
                return await base.ShowDialogAsync();
            }
            else
            {
                var imported = 0;
                foreach (var modelImport in modelImports)
                {
                    if (Settings.Components.Any(x => x.Name == modelImport.Name))
                        continue;

                    imported++;
                    modelImport.Id = modelId++;
                    Settings.Components.Add(modelImport);
                }

                await DialogService.ShowMessageAsync("Import Complete", $"{imported}/{modelImports.Length} Components Imported.");
                return true;
            }
        }


        protected override Task SaveAsync()
        {
            var index = Settings.Components.Count;
            if (IsUpdateMode)
            {
                index = Settings.Components.IndexOf(_originalComponentModel);
                Settings.Components.Remove(_originalComponentModel);
            }
            Settings.Components.Insert(index, ComponentModel);
            ComponentModel.Initialize(Settings);
            return base.SaveAsync();
        }


        protected override bool CanExecuteSave()
        {
            if (ComponentModel == null)
                return false;

            Errors.Clear();
            foreach (var inputError in GetValidationErrors())
                Errors.Add(inputError);

            return Errors.Count == 0 && base.CanExecuteSave();
        }


        protected override Task CancelAsync()
        {
            ComponentModel = default;
            _originalComponentModel = null;
            return base.CancelAsync();
        }


        protected override async Task CloseAsync()
        {
            await base.CloseAsync();
        }


        private IEnumerable<string> GetValidationErrors()
        {
            // Name
            if (string.IsNullOrWhiteSpace(ComponentModel.Name))
                yield return "Name cannot be empty";
            if (!IsUpdateMode)
            {
                if (Settings.Components.Any(x => x.Name.Equals(ComponentModel.Name, StringComparison.OrdinalIgnoreCase)))
                    yield return $"Model with Name '{ComponentModel.Name}' already exists";
            }

            // Checkpoint
            if (!ComponentModel.Checkpoint.IsValid(out var checkpointValidation))
                yield return checkpointValidation;
            if (string.IsNullOrEmpty(ComponentModel.Checkpoint.MainFile))
                yield return "MainFile cannot be empty";
        }
    }
}
