using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MatthiasToolbox.Basics.Datastructures.Graph;
using MatthiasToolbox.GraphDesigner;
using MatthiasToolbox.GraphDesigner.Controls;
using MatthiasToolbox.GraphDesigner.DiagramDesigner;
using MatthiasToolbox.GraphDesigner.Enumerations;
using MatthiasToolbox.GraphDesigner.Events;
using MatthiasToolbox.Logging;
using MatthiasToolbox.OntologyEditor.Semantics;
using MatthiasToolbox.OntologyEditor.Views;
using MatthiasToolbox.Presentation.Interfaces;
using MatthiasToolbox.Presentation.Utilities;
using MatthiasToolbox.Semantics.Metamodel;
using MatthiasToolbox.Semantics.Metamodel.Layout;

namespace MatthiasToolbox.OntologyEditor
{
    /// <summary>
    /// Interaction logic for OntologyControl.xaml
    /// </summary>
    public partial class OntologyControl : UserControl
    {
        #region cvar

        private Ontology _onto;
        private View _view;

        private VertexToolbox conceptPalette;
        private VertexToolbox relationPalette;

        #endregion
        #region prop

        public Ontology Ontology
        {
            get { return _onto; }
            set
            {
                if (value == _onto) return;
                UnloadOntology();
                if (value == null) return;
                _onto = value;
                InitializeOntology();
            }
        }

        public View View
        {
            get { return _view; }
            set
            {
                _view = value;
                // TODO:    dwi: reload visuals = 
                //InitializeView();

            }
        }
        public bool IsReadOnly { get; set; }

        #endregion
        #region ctor

        public OntologyControl()
        {
            InitializeComponent();

            this.DataContextChanged += new DependencyPropertyChangedEventHandler(OntologyControl_DataContextChanged);

            InitializeOntology();
        }

        #endregion
        #region init

        private void InitializeOntology()
        {
            if (_onto == null) return;

            if (!_onto.IsInitialized)
            {
                if (!_onto.Initialize(false)) return;
            }

            if (_onto.ViewTable.Count() > 0)
            {
                _view = _onto.ViewTable.First();
            }
            else
            {
                _view = new View("Default View");
                if (!IsReadOnly)
                {
                    _onto.ViewTable.InsertOnSubmit(_view);
                    _onto.SubmitChanges();
                }
            }

            HideDefaultPalettes();

            InitializeView();

            // listen to events
            graphControl1.VertexCreated += graphControl1_VertexCreated;
            graphControl1.EdgeCreated += graphControl1_EdgeCreated;

            graphControl1.OptionsDialogExecuted += graphControl1_OptionsDialogExecuted;

            graphControl1.VertexChanged += new GraphControl.VertexChangedHandler(graphControl1_VertexChanged);
            graphControl1.EdgeChanged += new GraphControl.EdgeChangedHandler(graphControl1_EdgeChanged);

            graphControl1.VertexRemoved += new GraphControl.VertexRemovedHandler(graphControl1_VertexRemoved);
            graphControl1.EdgeRemoved += new GraphControl.EdgeRemovedHandler(graphControl1_EdgeRemoved);
        }

        private void InitializeView()
        {
            InitFactories();

            // TODO  OntologyEditor - mag: you know what. (maybe only in design mode); the following methods set all concept.view and all relation.view properties !?!
            AddConceptPalette(); // add custom stencils (concept)
            AddRelationPalette(); // add custom stencils (relation)

            LoadView();
        }

        private void LoadView()
        {
            graphControl1.Graph = _view;
            graphControl1.Load();

            foreach (Connection connection in graphControl1.Connections)
            {
                UpdateConnectionValues(connection);
            }
        }

        /// <summary>
        /// Initilize the Node and Edge factories
        /// </summary>
        private void InitFactories()
        {
            graphControl1.NodeFactory = new SemanticNodeFactory(_onto, _view);
            graphControl1.EdgeFactory = new SemanticEdgeFactory(_onto, _view);
        }

        #endregion
        #region hand

        private void graphControl1_OptionsDialogExecuted(object sender, OptionsDialogEventArgs e)
        {
            if (e.SelectedVertex != null)
            {
                if (e.SelectedVertex is Concept)
                {
                    Concept concept = e.SelectedVertex as Concept;
                    ConceptOptionsDialog dialog = new ConceptOptionsDialog(_onto, concept);
                    bool? result = dialog.ShowDialog();
                    if (result == true)
                    {
                        UpdateConceptTemplate(concept);
                        graphControl1.RefreshData();
                    }

                }
            }
            else if (e.SelectedEdge != null)
            {
                EdgeOptionsDialog dialog = new EdgeOptionsDialog();
                dialog.DataContext = e.SelectedEdge;
                bool? result = dialog.ShowDialog();
                if (result == true)
                {
                    foreach (Connection connection in e.SelectedObjects.OfType<Connection>())
                    {
                        UpdateConnectionValues(connection);
                    }
                    UpdateRelationTemplate(e.SelectedEdge as Relation);
                }
            }
        }

        /// <summary>
        /// Called when [control_ data context changed].
        /// Sets the ontology and calls the IntializeOntology method.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void OntologyControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null && !(e.NewValue is Ontology))
            {
                Logger.Log<ERROR>("MatthiasToolbox.OntologyEditor.OntologyControl.OntologyControl_DataContextChanged", 
                    String.Format("DataContext has to be type {0}", typeof(Ontology).FullName));
                return;
            }

            UnloadOntology();

            //initialize ontology
            _onto = e.NewValue as Ontology;
            InitializeOntology();
        }

        private void RelationPaletteOptions_Executed(object sender, PaletteOptionsEventArgs options)
        {
            RelationTemplateDialog relationTemplateDialog = new RelationTemplateDialog(_view, options.Palette);
            relationTemplateDialog.ShowDialog();
        }

        private void ZoomBox_ZoomRectangleClick(object sender, RoutedEventArgs e)
        {
            this.graphControl1.SetGraphState(MouseUsageStates.ZoomRectangle, true);
        }

        #region vertices

        private void graphControl1_VertexCreated(object sender, VertexEventArgs e)
        {
            (e.Vertex as Concept).ViewContext = _view;
            (conceptPalette.ItemsSource as ObservableCollection<ConceptTemplate>).Add(new ConceptTemplate(e.Vertex as Concept));
        }

        private void graphControl1_VertexRemoved(object sender, VertexEventArgs e)
        {
            if (e.Vertex == null)
                return;

            (conceptPalette.ItemsSource as ObservableCollection<ConceptTemplate>).Remove(new ConceptTemplate(e.Vertex as Concept));
        }

        private void graphControl1_VertexChanged(object sender, VertexEventArgs e)
        {
            var items = from o in graphControl1.DesignerItems where o.Vertex == e.Vertex select o;
            foreach (DesignerItem designerItem in items)
            {
                UpdateConceptLayoutData(designerItem);
            }
        }

        #endregion
        #region edges

        private void graphControl1_EdgeCreated(object sender, EdgeEventArgs e)
        {
            // TODO  OntologyEditor - check if it already exists!!!
            (e.Edge as Relation).ViewContext = _view;
            //(relationPalette.ItemsSource as ObservableCollection<RelationTemplate>).Add(new RelationTemplate(e.Edge as Relation));
        }

        private void graphControl1_EdgeRemoved(object sender, EdgeEventArgs e)
        {
            if (e.Edge == null)
                return;

            (relationPalette.ItemsSource as ObservableCollection<RelationTemplate>).Remove(new RelationTemplate(e.Edge as Relation));
        }

        private void graphControl1_EdgeChanged(object sender, EdgeEventArgs e)
        {

        }

        #endregion

        #endregion
        #region impl

        /// <summary>
        /// Unloads the ontology. Closes the connections to the database.
        /// </summary>
        private void UnloadOntology()
        {
            if (_onto == null) return;

            _onto.Close();
            _onto = null;
        }

        /// <summary>
        /// Close the Ontology
        /// </summary>
        public void Close()
        {
            UpdateGraphLayoutData();

            UnloadOntology();
        }

        #region update

        private void UpdateConnectionValues(Connection connection)
        {
            Relation relation = connection.Edge as Relation;
            if(relation == null)
                Logger.Log<ERROR>("MatthiasToolbox.OntologyEditor.OntologyControl.UpdateConnection",
                    String.Format("Error updating the connection {0}.", connection));

            connection.SourceArrowSymbol = (ArrowSymbol)Enum.ToObject(typeof(ArrowSymbol), relation.StartCaps);
            connection.SinkArrowSymbol = (ArrowSymbol) Enum.ToObject(typeof (ArrowSymbol), relation.EndCaps);

            if (relation.IsDirected)
            {
                connection.SourceArrowSymbol = ArrowSymbol.None;
                connection.SinkArrowSymbol = ArrowSymbol.Arrow;
            }
            
            connection.Routing = (PathRouting) Enum.ToObject(typeof(PathRouting), relation.LineType);
            connection.UpdatePathGeometry();
        }

        private void UpdateConceptTemplate(Concept concept)
        {
            var conceptTemplates = (from o in conceptPalette.ItemsSource.OfType<ConceptTemplate>() where o.BaseConcept == concept select o).ToArray();
            foreach (ConceptTemplate template in conceptTemplates)
            {
                (conceptPalette.ItemsSource as ObservableCollection<ConceptTemplate>).Remove(template);
            }
            (conceptPalette.ItemsSource as ObservableCollection<ConceptTemplate>).Add(new ConceptTemplate(concept));
        }

        /// <summary>
        /// Updates the templates to match the changed relation.
        /// </summary>
        /// <param name="relation"></param>
        private void UpdateRelationTemplate(Relation relation)
        {
            var relationTemplates = (from o in conceptPalette.ItemsSource.OfType<RelationTemplate>() where o.Name == relation.Name select o).ToArray();
            foreach (RelationTemplate template in relationTemplates)
            {
                (relationPalette.ItemsSource as ObservableCollection<RelationTemplate>).Remove(template);
            }
            (relationPalette.ItemsSource as ObservableCollection<RelationTemplate>).Add(new RelationTemplate(relation));
        }

        /// <summary>
        /// Updates the data of the ontology children
        /// </summary>
        private void UpdateGraphLayoutData()
        {
            foreach (DesignerItem item in this.graphControl1.DesignerItems)
            {
                UpdateConceptLayoutData(item);
            }
        }

        private static void UpdateConceptLayoutData(DesignerItem item)
        {
            Concept concept = item.Vertex as Concept;
            if (concept == null)
                return;
            concept.Position = item.Position;
            concept.Size = item.ActualSize;
        }

        #endregion
        #region palette

        /// <summary>
        /// Add a RelationTemplate palette. //TODO  OntologyEditor - Store Templates
        /// </summary>
        private void AddRelationPalette()
        {
            List<RelationTemplate> relationStencils = new List<RelationTemplate>();

            RelationTemplate newRelationTemplate = new RelationTemplate();
            newRelationTemplate.Name = "new relation";
            newRelationTemplate.ForegroundColor = Colors.Black;
            newRelationTemplate.BackgroundColor = Colors.Gray;
            relationStencils.Add(newRelationTemplate);

            foreach (Relation relation in _onto.RelationTable)
            {
                relation.ViewContext = _view;
                relationStencils.Add(new RelationTemplate(relation));
            }

            relationPalette = AddPalette<RelationTemplate>("Relations", relationStencils, RelationPaletteOptions_Executed);
        }

        private void AddConceptPalette()
        {
            List<ConceptTemplate> termStencils = new List<ConceptTemplate>();

            foreach (Concept c in _onto.ConceptTable)
            {
                c.ViewContext = _view;
                termStencils.Add(new ConceptTemplate(c));
            }

            conceptPalette = AddPalette<ConceptTemplate>("Concepts", termStencils);
        }

        public void HideDefaultPalettes()
        {
            expanderFlowChart.Visibility = Visibility.Collapsed;
            expanderShapes.Visibility = Visibility.Collapsed;
            expanderAutomation.Visibility = Visibility.Collapsed;
            expanderSymbols.Visibility = Visibility.Collapsed;
        }

        public void ShowDefaultPalettes()
        {
            expanderFlowChart.Visibility = Visibility.Visible;
            expanderShapes.Visibility = Visibility.Visible;
            expanderAutomation.Visibility = Visibility.Visible;
            expanderSymbols.Visibility = Visibility.Visible;
        }

        public VertexToolbox AddPalette<T>(string title, List<T> items, GraphControl.PaletteOptionsHandler paletteOptionsHandler = null, bool isExpanded = true)
        {
            if (items.Count == 0)
            {
                this.Log<ERROR>("Unable to create palette - the provided list of items is empty. Add at least one dummy element.");
                return null;
            }

            VertexToolbox vtb = new VertexToolbox();
            vtb.Title = title;
            vtb.SnapsToDevicePixels = true;
            vtb.AddMultipleItemStart += this.graphControl1.Toolbox_AddMultipleItemStart;
            vtb.AddMultipleItemEnd += this.graphControl1.Toolbox_AddMultipleItemEnd;
            vtb.AddSingleItem += this.graphControl1.Toolbox_AddSingleItem;
            vtb.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            vtb.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);

            ObservableCollection<T> obsItems = new ObservableCollection<T>(items);
            vtb.ItemsSource = obsItems;


            Expander exp = new Expander();
            exp.Style = FindResource("GraphControlExpander") as Style;
            if (paletteOptionsHandler == null)
                exp.Header = title;
            else
            {//generate header text and button

                DockPanel dockPanel = new DockPanel();
                dockPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
                dockPanel.LastChildFill = false;

                //headline
                TextBlock textBlock = new TextBlock();
                textBlock.Text = title;
                dockPanel.Children.Add(textBlock);

                //options button
                Button button = new Button();
                DockPanel.SetDock(button, Dock.Right);
                button.Content = "Options";
                button.BorderThickness = new Thickness(0);
                button.Style = Resources[ToolBar.ButtonStyleKey] as Style;
                button.Click += new RoutedEventHandler(delegate
                {
                    paletteOptionsHandler.Invoke(vtb, new PaletteOptionsEventArgs(vtb));
                });
                dockPanel.Children.Add(button);

                exp.Header = dockPanel;
            }
            exp.IsExpanded = isExpanded;
            exp.Content = vtb;

            stackPanelPalette.Children.Add(exp);

            return vtb;
        }

        #endregion

        #endregion
    }
}