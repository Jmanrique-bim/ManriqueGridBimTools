using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Windows;

namespace ManriqueBimTools
{
    /// <summary>
    /// Interaction logic for LocateInstanceByGridForm.xaml
    /// </summary>
    public partial class LocateInstanceByGridForm : Window
    {
        private UIDocument _uidoc;

        // These properties store the UI selections.
        public bool NumberOnXAxis { get; private set; }
        public bool NumberOnYAxis { get; private set; }
        public bool NumberByProximity { get; private set; }

        // Indicates whether the user already selected the first instance.
        public bool FirstInstanceSelected { get; private set; }
        // The ElementId of the selected first instance.
        public Autodesk.Revit.DB.ElementId SelectedFirstInstance { get; private set; }

        // Accept UIDocument so we can prompt for selection.
        public LocateInstanceByGridForm(UIDocument uidoc)
        {
            InitializeComponent();
            _uidoc = uidoc;
        }

        // Event handler for the "Select First Instance" button.
        private void btnSelectFirstInstance_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Prompt the user to pick the first instance.
                Autodesk.Revit.DB.Reference r = _uidoc.Selection.PickObject(ObjectType.Element, "Select the first instance for numbering");
                if (r != null)
                {
                    SelectedFirstInstance = r.ElementId;
                    FirstInstanceSelected = true;
                    MessageBox.Show("First instance selected.");
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                FirstInstanceSelected = false;
            }
        }

        // OK button – store the checkbox states.
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            NumberOnXAxis = chkNumberXAxis.IsChecked == true;
            NumberOnYAxis = chkNumberYAxis.IsChecked == true;
            NumberByProximity = chkNumberByProximity.IsChecked == true;
            DialogResult = true;
            Close();
        }

        // Cancel button.
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
