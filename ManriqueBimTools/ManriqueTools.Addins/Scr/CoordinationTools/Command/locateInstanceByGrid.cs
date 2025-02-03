using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManriqueBimTools
{
    [Transaction(TransactionMode.Manual)]
    public class locateInstanceByGrid : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the Revit application, UIDocument, and Document.
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Determine the first instance.
            Element firstElem = null;
            
            // Fallback: prompt the user.
            Reference firstRef;
            try
            {
                firstRef = uidoc.Selection.PickObject(ObjectType.Element, "Select the first instance for numbering");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            firstElem = doc.GetElement(firstRef);
            
            if (firstElem == null)
            {
                TaskDialog.Show("Error", "Could not retrieve the selected element.");
                return Result.Failed;
            }

            LocateInstanceByGridForm form = new LocateInstanceByGridForm(uidoc);

            // Show the dialog modally.
            bool? result = form.ShowDialog();

            if (result != true) return Result.Cancelled;

            // Retrieve the options selected by the user.
            bool numberOnXAxis = form.NumberOnXAxis;
            bool numberOnYAxis = form.NumberOnYAxis;
            bool numberByProximity = form.NumberByProximity;
            bool firstInstanceSelected = form.FirstInstanceSelected;

            // Get the category of the first element.
            BuiltInCategory builtCat = firstElem.Category.BuiltInCategory;
            Category firstCat = firstElem.Category;
            if (firstCat == null || builtCat == null)
            {
                TaskDialog.Show("Error", "The selected element does not belong to any category.");
                return Result.Failed;
            }

            // Create a collector for all family instances in the document that belong to the same category.
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfCategoryId(firstCat.Id)
                .OfClass(typeof(FamilyInstance))
                .WhereElementIsNotElementType();

            // Filter out nested family instances using the IsNested property.
            List<FamilyInstance> familyInstances = collector.Cast<FamilyInstance>()
                .Where(fi => fi.SuperComponent == null)
                .ToList();
            List<Element> selectedElements = familyInstances.Cast<Element>().ToList();

            // Build a CategorySet containing the first element's category.
            CategorySet catSet = uiapp.Application.Create.NewCategorySet();
            catSet.Insert(firstCat);

            // Ensure that the required shared parameters exist for this category.
            GridHelper.EnsureParameterExists(doc, "Grid Square", catSet);
            GridHelper.EnsureParameterExists(doc, "Number", catSet);

            // Collect all grid elements from the document.
            FilteredElementCollector gridCollector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Grids)
                .WhereElementIsNotElementType();
            List<Grid> grids = gridCollector.Cast<Grid>().ToList();

            // Separate grids into two groups: those whose names start with a letter and those starting with a digit.
            List<Grid> letterGrids = grids.Where(g => GridHelper.IsAlphabetic(g.Name)).ToList();
            List<Grid> numberGrids = grids.Where(g => GridHelper.IsNumeric(g.Name)).ToList();

            // Use the first element's location as the starting point.
            XYZ startPt = GridHelper.GetElementLocation(firstElem);
            if (startPt == null)
            {
                TaskDialog.Show("Error", "Could not determine the location of the first instance.");
                return Result.Failed;
            }

            List<Element> sortedElements = null;
            // Order based on the user’s selection:
            if (numberOnXAxis)
            {
                sortedElements = selectedElements.OrderBy(e => GridHelper.GetElementLocation(e)?.X ?? 0).ToList();
            }
            else if (numberOnYAxis)
            {
                sortedElements = selectedElements.OrderBy(e => GridHelper.GetElementLocation(e)?.Y ?? 0).ToList();
            }
            else if (numberByProximity)
            {
                sortedElements = selectedElements.OrderBy(e => GridHelper.GetElementLocation(e)?.DistanceTo(startPt) ?? double.MaxValue).ToList();
            }
            else
            {
                // Default to proximity if none is selected.
                sortedElements = selectedElements.OrderBy(e => GridHelper.GetElementLocation(e)?.DistanceTo(startPt) ?? double.MaxValue).ToList();
            }

            // Start a transaction to update the parameters.
            using (Transaction trans = new Transaction(doc, "Assign Grid Square and Number"))
            {
                trans.Start();
                int counter = 1;
                foreach (Element elem in sortedElements)
                {
                    XYZ elemPt = GridHelper.GetElementLocation(elem);
                    if (elemPt == null)
                        continue;

                    // Find the closest letter grid and number grid.
                    Grid closestLetter = letterGrids.OrderBy(g => g.Curve.Distance(elemPt)).FirstOrDefault();
                    Grid closestNumber = numberGrids.OrderBy(g => g.Curve.Distance(elemPt)).FirstOrDefault();
                    string gridSquare = "";
                    if (closestLetter != null && closestNumber != null)
                    {
                        gridSquare = closestLetter.Name + "-" + closestNumber.Name;
                    }

                    // Set the "Grid Square" parameter value.
                    Parameter gridParam = elem.LookupParameter("Grid Square");
                    if (gridParam != null && !gridParam.IsReadOnly)
                    {
                        gridParam.Set(gridSquare);
                    }

                    // Set the "Number" parameter value.
                    Parameter numParam = elem.LookupParameter("Number");
                    if (numParam != null && !numParam.IsReadOnly)
                    {
                        numParam.Set(counter.ToString());
                    }
                    counter++;
                }
                trans.Commit();
            }

            return Result.Succeeded;
        }
    }
}
