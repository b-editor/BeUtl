﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BEditor.ViewModels.Helper;

using BEditor.Core.Data;
using BEditor.Core.Data.PropertyData;

namespace BEditor.ViewModels.PropertyControl {
    public class DocumentPropertyViewModel {
        public DocumentProperty Property { get; }
        public DelegateCommand<string> TextChangeCommand { get; }

        public DocumentPropertyViewModel(DocumentProperty property) {
            Property = property;
            TextChangeCommand = new DelegateCommand<string>(x => {
                UndoRedoManager.Do(new DocumentProperty.TextChangeCommand(property, x));
            });
        }
    }
}
