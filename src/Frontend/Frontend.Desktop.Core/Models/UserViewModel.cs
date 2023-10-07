using Prism.Mvvm;

namespace Frontend.Desktop.Core
{
    public class UserViewModel : BindableBase
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public bool Online { get; set; }
        
    }
}