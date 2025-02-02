using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Linq;
using Autodesk.Revit.ApplicationServices;

namespace ManriqueBimTools
{
    /// <summary>
    /// Provides helper methods related to grid and element location operations.
    /// </summary>
    public static class GridHelper
    {
        /// <summary>
        /// Ensures that a shared text parameter with the given name exists and is bound to the specified categories.
        /// If the parameter does not exist, it is created using the shared parameter file.
        /// </summary>
        /// <param name="doc">The active document.</param>
        /// <param name="paramName">The name of the parameter to ensure.</param>
        /// <param name="catSet">The categories to bind the parameter to.</param>
        public static void EnsureParameterExists(Document doc, string paramName, CategorySet catSet)
        {
            using (Transaction t = new Transaction(doc, "Ensure Parameter " + paramName))
            {
                t.Start();

                // First, check if the parameter binding already exists in the document.
                DefinitionBindingMapIterator iter = doc.ParameterBindings.ForwardIterator();
                while (iter.MoveNext())
                {
                    Definition def = iter.Key as Definition;
                    if (def != null && def.Name.Equals(paramName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        t.Commit();
                        return;
                    }
                }

                Application app = doc.Application;
                // Open the shared parameter file.
                DefinitionFile defFile = app.OpenSharedParameterFile();
                if (defFile == null)
                {
                    TaskDialog.Show("Error", "No shared parameter file is defined. Please set one in Revit Options.");
                    t.RollBack();
                    return;
                }

                // Get (or create) a group named "ManriqueBimTools" in the shared parameter file.
                DefinitionGroup group = defFile.Groups.get_Item("ManriqueBimTools");
                if (group == null)
                {
                    group = defFile.Groups.Create("ManriqueBimTools");
                }

                // Check if a definition with the same name already exists in the group.
                Definition definition = null;
                foreach (Definition def in group.Definitions)
                {
                    if (def.Name.Equals(paramName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        definition = def;
                        break;
                    }
                }

                // If no existing definition was found, create a new one.
                if (definition == null)
                {
                    ExternalDefinitionCreationOptions options =
                        new ExternalDefinitionCreationOptions(paramName, Autodesk.Revit.DB.SpecTypeId.String.Text)
                        {
                            Visible = true
                        };

                    definition = group.Definitions.Create(options);
                }

                // Create an instance binding for the specified categories.
                InstanceBinding binding = app.Create.NewInstanceBinding(catSet);

                // Insert the new binding into the document using the two-argument Insert.
                bool success = doc.ParameterBindings.Insert(definition, binding);
                if (!success)
                {
                    TaskDialog.Show("Error", "Could not bind parameter: " + paramName);
                }
                else
                {
                    // Update the parameter group to Identity Data.
                    doc.ParameterBindings.ReInsert(definition, binding, BuiltInParameterGroup.PG_IDENTITY_DATA);
                }
                t.Commit();
            }
        }
        public static View3D CreatDummy3DView(Document doc)
        {
            var viewType = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
                .WhereElementIsElementType().Cast<ViewFamilyType>()
                .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);
            if (viewType != null)
            {
                return View3D.CreateIsometric(doc, viewType.Id);
            }

            return null;
        }

        /// <summary>
        /// Determines whether the first character of the input string is an alphabetic letter.
        /// </summary>
        /// <param name="input">The input string to check.</param>
        /// <returns>True if the first character is a letter; otherwise, false.</returns>
        public static bool IsAlphabetic(string input)
        {
            return !string.IsNullOrEmpty(input) && char.IsLetter(input[0]);
        }

        /// <summary>
        /// Determines whether the first character of the input string is a numeric digit.
        /// </summary>
        /// <param name="input">The input string to check.</param>
        /// <returns>True if the first character is a digit; otherwise, false.</returns>
        public static bool IsNumeric(string input)
        {
            return !string.IsNullOrEmpty(input) && char.IsDigit(input[0]);
        }

        /// <summary>
        /// Retrieves the location point of an element.
        /// If the element has a LocationPoint, that point is returned.
        /// If the element has a LocationCurve, the midpoint of the curve is returned.
        /// Returns null if neither is available.
        /// </summary>
        /// <param name="e">The element whose location is to be determined.</param>
        /// <returns>The XYZ point representing the element's location or null if not found.</returns>
        public static XYZ GetElementLocation(Element e)
        {
            if (e == null)
                return null;

            Location loc = e.Location;
            if (loc is LocationPoint lp)
            {
                return lp.Point;
            }
            else if (loc is LocationCurve lc)
            {
                Curve curve = lc.Curve;
                return curve.Evaluate(0.5, true);
            }
            return null;
        }
    }
}
