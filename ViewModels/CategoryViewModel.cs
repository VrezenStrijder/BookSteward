using System.Collections.ObjectModel;
using System.Windows.Input;
using BookSteward.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookSteward.ViewModels
{
    public partial class CategoryViewModel : ObservableObject
    {
        private readonly Category _category;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private bool isEditing;

        [ObservableProperty]
        private ObservableCollection<CategoryViewModel> children;

        public int Id => _category.Id;
        public int? ParentId => _category.ParentId;

        public CategoryViewModel(Category category)
        {
            _category = category;
            Name = category.Name;
            Children = new ObservableCollection<CategoryViewModel>();

            if (category.Children != null)
            {
                foreach (var child in category.Children)
                {
                    Children.Add(new CategoryViewModel(child));
                }
            }
        }

        public Category ToModel()
        {
            _category.Name = Name;
            return _category;
        }
    }
}