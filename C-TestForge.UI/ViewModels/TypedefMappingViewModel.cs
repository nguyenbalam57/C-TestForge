using C_TestForge.Models.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.UI.ViewModels
{
    public class TypedefMappingViewModel : ObservableObject
    {
        private string _userType;
        private string _baseType;
        private string _minValue;
        private string _maxValue;
        private int _size;
        private string _source;
        private bool _isEditable;

        public string UserType
        {
            get => _userType;
            set => SetProperty(ref _userType, value);
        }

        public string BaseType
        {
            get => _baseType;
            set => SetProperty(ref _baseType, value);
        }

        public string MinValue
        {
            get => _minValue;
            set => SetProperty(ref _minValue, value);
        }

        public string MaxValue
        {
            get => _maxValue;
            set => SetProperty(ref _maxValue, value);
        }

        public int Size
        {
            get => _size;
            set => SetProperty(ref _size, value);
        }

        public string Source
        {
            get => _source;
            set => SetProperty(ref _source, value);
        }

        public bool IsEditable
        {
            get => _isEditable;
            set => SetProperty(ref _isEditable, value);
        }

        public TypedefMappingViewModel(TypedefMapping model)
        {
            UserType = model.UserType;
            BaseType = model.BaseType;
            MinValue = model.MinValue;
            MaxValue = model.MaxValue;
            Size = model.Size;
            Source = model.Source;
            IsEditable = !Source.Equals("Predefined", StringComparison.OrdinalIgnoreCase);
        }

        public TypedefMapping ToModel()
        {
            return new TypedefMapping
            {
                UserType = UserType,
                BaseType = BaseType,
                MinValue = MinValue,
                MaxValue = MaxValue,
                Size = Size,
                Source = Source
            };
        }
    }
}
