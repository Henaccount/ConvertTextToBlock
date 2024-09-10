using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

public class TextToBlock
{
    [CommandMethod("ConvertTextToBlock")]
    public static void ConvertTextToBlock()
    {
        Document doc = Application.DocumentManager.MdiActiveDocument;
        Database db = doc.Database;
        Editor ed = doc.Editor;

        //avoid snapping when executing the "insert" command, remember the snaps, turn them off an on again after the script execution
        object snapmodesaved = Application.GetSystemVariable("SNAPMODE");
        object osmodesaved = Application.GetSystemVariable("OSMODE");
        object os3dmodesaved = Application.GetSystemVariable("3DOSMODE");

        Application.SetSystemVariable("SNAPMODE", 0);
        Application.SetSystemVariable("OSMODE", 0);
        Application.SetSystemVariable("3DOSMODE", 0);

        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            BlockTableRecord modelSpace = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
            string text = "xxxx";
            foreach (ObjectId id in modelSpace)
            {
                DBObject obj = tr.GetObject(id, OpenMode.ForRead);

                if (obj is DBText || obj is MText)
                {
                    text = (obj is DBText) ? ((DBText)obj).TextString : ((MText)obj).Contents;
                    if(text.Length > 30) text = text.Substring(0, 30);
                    Point3d position = (obj is DBText) ? ((DBText)obj).Position : ((MText)obj).Location;

                    System.Guid guid = System.Guid.NewGuid();

                    ed.Command("_-block", guid.ToString(), position, obj.ObjectId, "");
                    ed.Command("_-insert", guid.ToString(), position, 1, 1, 0);

                    PromptSelectionResult lres = ed.SelectLast();

                    BlockReference blockRef = tr.GetObject(lres.Value.GetObjectIds()[0], OpenMode.ForWrite) as BlockReference;
                    BlockTableRecord blockDef = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;

                    AttributeDefinition attDef = new AttributeDefinition();
                    attDef.Position = position; // Set the position of the attribute
                    attDef.Tag = "tag";
                    attDef.Prompt = "";
                    attDef.TextString = text;
                    //show property in palette
                    attDef.Visible = true;
                    //dont show tag label in drawing area
                    attDef.Invisible = true;

                    // Add the attribute definition to the block definition
                    blockDef.AppendEntity(attDef);
                    tr.AddNewlyCreatedDBObject(attDef, true);

                    // Add the attribute reference to the block reference
                    AttributeReference attRef = new AttributeReference();
                    attRef.SetAttributeFromBlock(attDef, blockRef.BlockTransform);
                    attRef.Position = attDef.Position;
                    attRef.TextString = text;
                    attRef.Visible = true;
                    attRef.Invisible = true;

                    blockRef.AttributeCollection.AppendAttribute(attRef);
                    tr.AddNewlyCreatedDBObject(attRef, true);

                }
            }

            tr.Commit();

        }
        Application.SetSystemVariable("SNAPMODE", snapmodesaved);
        Application.SetSystemVariable("OSMODE", osmodesaved);
        Application.SetSystemVariable("3DOSMODE", os3dmodesaved);
    }
}
