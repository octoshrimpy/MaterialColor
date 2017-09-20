﻿using Common.Data;
using Common.IO;
using Common.Json;
using Injector.IO;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;

namespace Injector.WPF.ViewModels
{
    public class InjectorViewModel : BindableBase
    {
        public InjectorViewModel(InjectorStateManager stateManager, FileManager fileManager, InjectionManager injector)
        {
            _logger = new Logger(Common.Paths.InjectorLogFileName);

            PatchCommand = new DelegateCommand(Patch, CanPatch);
            RestoreBackupCommand = new DelegateCommand(RestoreBackup, CanRestoreBackup);
            ExitCommand = new DelegateCommand(App.Current.Shutdown);

            _stateManager = stateManager;
            _fileManager = fileManager;
            _injector = injector;

            State = TryLoadLastAppState();

            if (!IsCSharpPatched && CanRestoreCSharpBackup())
            {
                Status = "Warning: A backup for Assembly-CSharp.dll exists, but current assembly doesn't appear to be patched. Patching without restoring backup is advised.";
            }

            if (!IsFirstpassPatched && CanRestoreFirstpassBackup())
            {
                Status = "Warning: A backup for Assembly-CSharp-firstpass.dll exists, but current assembly doesn't appear to be patched. Patching without restoring backup is advised.";
            }
        }

        public DelegateCommand PatchCommand { get; private set; }
        public DelegateCommand RestoreBackupCommand { get; private set; }
        public DelegateCommand ExitCommand { get; private set; }

        public string Status
        {
            get => _status;
            set
            {
                SetProperty(ref _status, $"{_status}\n{value}".Trim());
                _logger.Log(value);
            }
        }

        public InjectorState State
        {
            get => _state;
            set
            {
                SetProperty(ref _state, value);
                PatchCommand.RaiseCanExecuteChanged();
            }
        }

        private InjectorState _state;

        private InjectorStateManager _stateManager;
        private FileManager _fileManager;
        private InjectionManager _injector;
        private Logger _logger;

        private string _status;

        public bool CanRestoreBackup()
            => CanRestoreCSharpBackup() || CanRestoreFirstpassBackup();

        public bool CanRestoreCSharpBackup()
            => _fileManager.BackupForFileExists(Paths.DefaultAssemblyCSharpPath);

        public bool CanRestoreFirstpassBackup()
            => _fileManager.BackupForFileExists(Paths.DefaultAssemblyFirstPassPath);

        public bool IsCSharpPatched
            => _injector.IsCurrentAssemblyCSharpPatched();

        public bool IsFirstpassPatched
            => _injector.IsCurrentAssemblyFirstpassPatched();

        public bool CanPatch() => State.InjectMaterialColor || State.InjectOnion || State.EnableDebugConsole;

        public void Patch()
        {
            Status = $"[{DateTime.Now.TimeOfDay}] Patching started.";

            if (CanRestoreCSharpBackup())
            {
                if (IsCSharpPatched)
                {
                    if (TryRestoreCSharpBackup())
                    {
                        Status = "\tAssembly-CSharp.dll backup restored.";
                    }
                    else
                    {
                        Status = "\tAssembly-CSharp.dll backup failed.\n\tPatch cancelled.";
                        return;
                    }
                }
                else
                {
                    Status = "\tAssembly-CSharp.dll backup restore SKIPPED.";
                }
            }

            if (CanRestoreFirstpassBackup())
            {
                if (IsFirstpassPatched)
                {
                    if (TryRestoreFirstpassBackup())
                    {
                        Status = "\tAssembly-CSharp-firstpass.dll backup restored.";
                    }
                    else
                    {
                        Status = "\tAssembly-CSharp-firstpass.dll backup failed.\n\tPatch cancelled.";
                        return;
                    }
                }
                else
                {
                    Status = "\tAssembly-CSharp-firstpass.dll backup restore SKIPPED.";
                }
            }

            try
            {
                _injector.InjectDefaultAndBackup(State);
            }
            catch (Exception e)
            {
                Status = "\tInjection failed.";
                _logger.Log(e);

                return;
            }

            RestoreBackupCommand.RaiseCanExecuteChanged();

            Status = "\tOriginal backed up.\n\tOriginal patched.\n\tPatch successful.";

            try
            {
                IOHelper.EnsureDirectoryExists(Common.Paths.MaterialConfigPath);
            }
            catch (Exception e)
            {
                Status = "Can't create or access directory for state to save.";
                _logger.Log(e);

                return;
            }

            try
            {
                _stateManager.SaveState(State);
            }
            catch (Exception e)
            {
                Status = "Can't save app state.";
                _logger.Log(e);

                return;
            }
        }

        public void RestoreBackup()
        {
            RestoreCSharpBackup();
            RestoreFirstpassBackup();
        }

        public void RestoreCSharpBackup()
        {
            if (TryRestoreCSharpBackup())
            {
                Status = $"[{DateTime.Now.TimeOfDay}] Assembly-CSharp.dll backup restore successful.";
            }
            else
            {
                Status = $"[{DateTime.Now.TimeOfDay}] Assembly-CSharp.dll backup restore failed.";
            }
        }

        public void RestoreFirstpassBackup()
        {
            if (TryRestoreFirstpassBackup())
            {
                Status = $"[{DateTime.Now.TimeOfDay}] Assembly-CSharp-firstpass.dll backup restore successful.";
            }
            else
            {
                Status = $"[{DateTime.Now.TimeOfDay}] Assembly-CSharp-firstpass.dll backup restore failed.";
            }
        }

        public bool TryRestoreCSharpBackup()
        {
            bool result = false;

            try
            {
                result = _fileManager.RestoreBackupForFile(Paths.DefaultAssemblyCSharpPath);
            }
            catch (Exception e)
            {

                Status = "Can't restore Assembly-CSharp.dll backup.";
                _logger.Log(e);

                result = false;
            }

            RestoreBackupCommand.RaiseCanExecuteChanged();

            return result;
        }

        public bool TryRestoreFirstpassBackup()
        {
            bool result = false;

            try
            {
                result = _fileManager.RestoreBackupForFile(Paths.DefaultAssemblyFirstPassPath);
            }
            catch (Exception e)
            {
                Status = "Can't restore Assembly-CSharp-firstpass.dll backup.";
                _logger.Log(e);

                result = false;
            }

            RestoreBackupCommand.RaiseCanExecuteChanged();

            return result;
        }

        [Obsolete]
        public bool TryRestoreBackup()
        {
            bool result = false;

            try
            {
                result = _fileManager.RestoreBackupForFile(Paths.DefaultAssemblyCSharpPath)
                    | _fileManager.RestoreBackupForFile(Paths.DefaultAssemblyFirstPassPath);
            }
            catch (Exception e)
            {
                Status = $"Can't restore backup.";
                _logger.Log(e);

                result = false;
            }

            RestoreBackupCommand.RaiseCanExecuteChanged();

            return result;
        }

        private InjectorState TryLoadLastAppState()
        {
            try
            {
                return _stateManager.LoadState();
            }
            catch (Exception e)
            {
                Status = "Can't load last state.";
                _logger.Log(e);

                return new InjectorState();
            }
        }
    }
}