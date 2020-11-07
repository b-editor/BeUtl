﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BEditor.ViewModels.Helper;

using BEditor.Core.Data;
using BEditor.Core.Data.PropertyData;

namespace BEditor.ViewModels.PropertyControl {
    public class CheckPropertyViewModel {
        public CheckProperty Property { get; }
        public DelegateCommand<bool> Command { get; }

        public CheckPropertyViewModel(CheckProperty property) {
            Property = property;
            Command = new DelegateCommand<bool>(x => {
                UndoRedoManager.Do(new CheckProperty.ChangeCheckedCommand(property, x));
            });
        }
    }
}
