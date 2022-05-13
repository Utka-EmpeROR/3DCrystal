using System;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ChemSharp.Molecules;

namespace _3DCrystal
{
    // Mercury
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private ChemSharp.Molecules.Molecule CurrentMolecule = null;

        // The currently selected model.
        private GeometryModel3D SelectedModel = null;
        private Atom SelectedAtom = null;


        // The list of selectable models.
        private List<GeometryModel3D> SelectableModels =
            new List<GeometryModel3D>();

        // Materials used for normal and selected models.
        private Material SelectedMaterial = new DiffuseMaterial(Brushes.Violet);

        // The camera.
        private PerspectiveCamera TheCamera = null;

        // The camera controller.
        private SphericalCameraController CameraController = null;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mainViewport.Children.Clear();
            // Define WPF objects.
            ModelVisual3D visual3d = new ModelVisual3D();
            Model3DGroup group = new Model3DGroup();
            visual3d.Content = group;
            mainViewport.Children.Add(visual3d);

            // Define the camera, lights, and model.
            DefineCamera(mainViewport);
            DefineLights(group);
            DefineModel(group);
        }

        // Define the camera.
        private void DefineCamera(Viewport3D viewport)
        {
            TheCamera = new PerspectiveCamera();
            TheCamera.FieldOfView = 60;
            CameraController = new SphericalCameraController
                (TheCamera, viewport, this, mainGrid, mainGrid);
        }

        // Define the lights.
        private void DefineLights(Model3DGroup group)
        {
            Color dark = Color.FromArgb(255, 96, 96, 96);

            group.Children.Add(new AmbientLight(dark));

            group.Children.Add(new DirectionalLight(dark, new Vector3D(0, -1, 0)));
            group.Children.Add(new DirectionalLight(dark, new Vector3D(1, -3, -2)));
            group.Children.Add(new DirectionalLight(dark, new Vector3D(-1, 3, 2)));
        }

        // Define the model.
        private void DefineModel(Model3DGroup group)
        {
            SelectableModels.Clear();
            if(CurrentMolecule == null)
            {

                Point3D point1 = new Point3D(-3, 2, 2);
                Point3D point2 = new Point3D(-1, -1, -1);
                Point3D point3 = new Point3D(2, 1, -1);

                MeshGeometry3D mesh1 = new MeshGeometry3D();
                mesh1.AddSphere(point1, 1, 20, 10);
                group.Children.Add(mesh1.MakeModel(Brushes.Green));
                SelectableModels.Add(mesh1.MakeModel(Brushes.Green));


                MeshGeometry3D mesh2 = new MeshGeometry3D();
                mesh2.AddSphere(point2, 0.5, 20, 10);
                group.Children.Add(mesh2.MakeModel(Brushes.Green));
                SelectableModels.Add(mesh2.MakeModel(Brushes.Green));

                MeshGeometry3D mesh3 = new MeshGeometry3D();
                mesh3.AddSphere(point3, 1, 20, 10);
                group.Children.Add(mesh3.MakeModel(Brushes.Green));
                SelectableModels.Add(mesh3.MakeModel(Brushes.Green));

                MeshGeometry3D mesh4 = new MeshGeometry3D();
                mesh4.AddLine(point1, point2);
                mesh4.AddLine(point3, point2);
                group.Children.Add(mesh4.MakeModel(Brushes.Pink));

                // Show the axes.
                MeshExtensions.AddAxes(group);
                return;
            }

            foreach (var atom in CurrentMolecule.Atoms)
            {
                double rad = atom.AtomicRadius != null ? (double)atom.AtomicRadius/2000 : 0.04;
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.AddSphere(new Point3D(atom.Location.X, atom.Location.Y, atom.Location.Z), rad, 20, 10);
                var model = mesh.MakeModel(Brushes.Green);
                ApplyColor(model, atom.Color);
                group.Children.Add(model);
                SelectableModels.Add(model);
            }
            foreach(var bond in CurrentMolecule.Bonds)
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                mesh.AddLine(new Point3D(bond.Atom1.Location.X, bond.Atom1.Location.Y, bond.Atom1.Location.Z),
                    new Point3D(bond.Atom2.Location.X, bond.Atom2.Location.Y, bond.Atom2.Location.Z));
                var model = mesh.MakeModel(Brushes.Pink);
                group.Children.Add(model);
            }
        }

        private void ApplyColor(GeometryModel3D model, string color)
        {
            Brush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            DiffuseMaterial mat = new DiffuseMaterial(brush);
            model.Material = mat;
        }
        
        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if(dialog.ShowDialog() == true)
            {
                if(System.IO.Path.GetExtension(dialog.FileName)!= ".cif")
                {
                    MessageBox.Show("Некорректный формат файла! Требуется файл с расширением .cif");
                    return;
                }
                ChemSharp.Molecules.DataProviders.CIFDataProviders provider;
                try
                {
                    provider = new ChemSharp.Molecules.DataProviders.CIFDataProviders(dialog.FileName);
                }
                catch
                {
                    MessageBox.Show("Невозможно считать файл");
                    return;
                }
                var mol = new ChemSharp.Molecules.Molecule(provider.Atoms, provider.Bonds);
                string result = mol.Atoms.Count.ToString() + ":" + Environment.NewLine;
                foreach(var atom in mol.Atoms)
                {
                   result+= atom.ToString() + Environment.NewLine;
                }
                MessageBox.Show(result);
                CurrentMolecule = mol;
                Window_Loaded(sender, e);
                FileTextBox.Text = dialog.FileName;
            }
        }


        #region Hit Testing Code

        // See what was clicked.
        private void MainViewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Deselect the prevously selected model.
            if (SelectedModel != null)
            {
                if (SelectedAtom != null)
                {
                    ApplyColor(SelectedModel, SelectedAtom.Color);
                    SelectedAtom = null;
                    ClearAtomInfo();
                }
                else
                {
                    SelectedModel.Material = new DiffuseMaterial(Brushes.Green);
                }
                SelectedModel = null;
            }

            // Get the mouse's position relative to the viewport.
            Point mouse_pos = e.GetPosition(mainViewport);

            // Perform the hit test.
            HitTestResult result =
                VisualTreeHelper.HitTest(mainViewport, mouse_pos);

            // See if we hit a model.
            RayMeshGeometry3DHitTestResult mesh_result =
                result as RayMeshGeometry3DHitTestResult;
            if (mesh_result != null)
            {
                GeometryModel3D model = (GeometryModel3D)mesh_result.ModelHit;
                if (SelectableModels.Exists(el => el.Bounds == model.Bounds))
                {
                    SelectedModel = model;
                    SelectedModel.Material = SelectedMaterial;
                    if(CurrentMolecule != null)
                    {
                        SelectedAtom = CurrentMolecule.Atoms[SelectableModels.IndexOf(model)];
                        ShowAtomInfo();
                    }
                }
            }
        }

        #endregion Hit Testing Code

        private void ClearAtomInfo()
        {
            AtomNameTextBox.Clear();
            AtomLocationTextBox.Clear();
            AtomNumberTextBox.Clear();
            AtomRadiusTextBox.Clear();
            AtomWeightTextBox.Clear();
            AtomPeriodTextBox.Clear();
            AtomSymbolTextBox.Clear();
            AtomCategory.Clear();
            AtomTitleTextBox.Clear();
            AtomElectronegativityTextBox.Clear();
            AtomGroupTextBox.Clear();
        }

        private void ShowAtomInfo()
        {
            AtomNameTextBox.Text = SelectedAtom.Name;
            AtomLocationTextBox.Text = SelectedAtom.Location.ToString();
            AtomNumberTextBox.Text = SelectedAtom.AtomicNumber.ToString();
            AtomRadiusTextBox.Text = SelectedAtom.AtomicRadius.ToString();
            AtomWeightTextBox.Text = SelectedAtom.AtomicWeight.ToString();
            AtomPeriodTextBox.Text = SelectedAtom.Period.ToString();
            AtomSymbolTextBox.Text = SelectedAtom.Symbol.ToString();
            AtomCategory.Text = SelectedAtom.Category;
            AtomTitleTextBox.Text = SelectedAtom.Title;
            AtomElectronegativityTextBox.Text = SelectedAtom.Electronegativity.ToString();
            AtomGroupTextBox.Text = SelectedAtom.Group.ToString();
        }
    }
}
