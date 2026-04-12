using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DATN_AUTO_CREATE_PART.Models;
using DATN_AUTO_CREATE_PART.Utils;
using System.Collections.ObjectModel;
using System.Linq;
using Tekla.Structures.Model.UI;
using TSM = Tekla.Structures.Model;

namespace DATN_AUTO_CREATE_PART.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<BeamInfoCollection> _beamCollections = new ObservableCollection<BeamInfoCollection>();

        [ObservableProperty]
        private ObservableCollection<ColumnInfoCollection> _columnCollections = new ObservableCollection<ColumnInfoCollection>();

        [ObservableProperty]
        private ObservableCollection<FloorInfoCollection> _floorCollections = new ObservableCollection<FloorInfoCollection>();

        [ObservableProperty]
        private GridInfo _gridSettings = new GridInfo();

        private XyzData _cadOrigin = null;
        private Tekla.Structures.Geometry3d.Point _teklaOrigin = null;
        private TSM.Model _model;

        public MainViewModel()
        {
            _model = new TSM.Model();
            BeamCollections.Clear();
            ColumnCollections.Clear();
            FloorCollections.Clear();
        }

        private bool EnsureCadOrigin()
        {
            if (_cadOrigin == null)
            {
                _cadOrigin = AutoCadInterop.GetCadOrigin();
                if (_cadOrigin == null)
                {
                    System.Windows.MessageBox.Show("CAD origin selection cancelled.", "Cancelled", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return false;
                }
            }
            return true;
        }

        [RelayCommand]
        private void ScanCadBeams()
        {
            if (!EnsureCadOrigin()) return;
            AutoCadInterop.ExtractBeams(out var extractedBeams, _cadOrigin);

            var grouped = extractedBeams.GroupBy(x => x.Text);
            BeamCollections.Clear();

            foreach (var group in grouped)
            {
                var col = new BeamInfoCollection
                {
                    Text = group.Key,
                    Number = group.Count(),
                    Width = 200, // default
                    Height = 400
                };
                
                foreach(var b in group)
                {
                    col.BeamInfos.Add(new BeamInfo(b.StartPoint, b.EndPoint, b.Text));
                }
                
                // Group duplicates within same collection
                col.BeamInfos = col.BeamInfos.Distinct(new BeamInfo.BeamInfoComparerByPoint()).ToList();
                col.Number = col.BeamInfos.Count;

                BeamCollections.Add(col);
            }
        }

        [RelayCommand]
        private void ScanCadColumns()
        {
            if (!EnsureCadOrigin()) return;
            AutoCadInterop.ExtractColumns(out var extractedColumns, _cadOrigin);

            var grouped = extractedColumns.GroupBy(x => x.Mask);
            ColumnCollections.Clear();

            foreach (var group in grouped)
            {
                var col = new ColumnInfoCollection
                {
                    Text = group.Key,
                    Number = group.Count()
                };

                foreach(var c in group)
                {
                    col.ColumnInfos.Add(new ColumnInfo(c.Points, c.Mask));
                }

                // Filter valid shapes and unique
                col.ColumnInfos = col.ColumnInfos.Distinct().ToList();
                
                if (col.ColumnInfos.Any())
                {
                    col.Width = col.ColumnInfos.First().Width;
                    col.Height = col.ColumnInfos.First().Height;
                    col.Number = col.ColumnInfos.Count;
                    ColumnCollections.Add(col);
                }
            }
        }

        [RelayCommand]
        private void ScanCadFloors()
        {
            if (!EnsureCadOrigin()) return;
            AutoCadInterop.ExtractFloors(out var extractedFloors, _cadOrigin);

            FloorCollections.Clear();

            foreach (var floor in extractedFloors)
            {
                var col = new FloorInfoCollection
                {
                    Area = floor.Area,
                    Number = 1
                };
                col.FloorPoints.Add(floor.Points);
                FloorCollections.Add(col);
            }
        }

        [RelayCommand]
        private void ScanCadGrids()
        {
            if (!EnsureCadOrigin()) return;
            AutoCadInterop.ExtractGrids(out string cX, out string cY, out string lX, out string lY, _cadOrigin);
            if (cX != "0" || cY != "0")
            {
                GridSettings.CoordinateX = cX;
                GridSettings.CoordinateY = cY;
                GridSettings.LabelX = lX;
                GridSettings.LabelY = lY;
            }
        }

        [RelayCommand]
        private void GenerateGridToTekla()
        {
            if (!_model.GetConnectionStatus())
            {
                System.Windows.MessageBox.Show("Please open Tekla Structures and a Model before running this command.", "Tekla Not Connected", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (_teklaOrigin == null)
            {
                WindowFocusHelper.BringToFront("TeklaStructures");
                var picker = new Picker();
                try
                {
                    _teklaOrigin = picker.PickPoint("Pick origin point in Tekla (once per project)");
                }
                catch (System.Exception ex) 
                { 
                    System.Windows.MessageBox.Show("Picking point cancelled or failed: " + ex.Message);
                    return; 
                }
            }

            if (_teklaOrigin != null)
            {
                TeklaInterop.GenerateStandardGrid(_teklaOrigin, GridSettings);
                System.Windows.MessageBox.Show("Grid generated successfully!", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void GenerateComponentsToTekla()
        {
            if (!_model.GetConnectionStatus())
            {
                System.Windows.MessageBox.Show("Please open Tekla Structures and a Model before running this command.", "Tekla Not Connected", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (_cadOrigin == null)
            {
                System.Windows.MessageBox.Show("CAD origin is missing. Please scan elements from CAD first.", "No CAD Data", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (_teklaOrigin == null)
            {
                WindowFocusHelper.BringToFront("TeklaStructures");
                var picker = new Picker();
                try
                {
                    _teklaOrigin = picker.PickPoint("Pick origin point in Tekla (once per project)");
                }
                catch (System.Exception ex) 
                { 
                    System.Windows.MessageBox.Show("Picking point cancelled or failed: " + ex.Message);
                    return; 
                }
            }

            if (_teklaOrigin != null)
            {
                TeklaInterop.GenerateBeams(BeamCollections, _cadOrigin, _teklaOrigin);
                TeklaInterop.GenerateColumns(ColumnCollections, _cadOrigin, _teklaOrigin);
                TeklaInterop.GenerateFloors(FloorCollections, _cadOrigin, _teklaOrigin);

                System.Windows.MessageBox.Show("Components generated successfully in Tekla!", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
    }
}
