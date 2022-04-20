using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SaveDocumentsToGMS
{
     public class WardDocumentUpload {
        
        public WardDocumentUpload() {
            // 
        }
        
        public string saveWardDocument(string ImageFilePath, string BasePath, string NewFileName) {
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
             
            string DocSaveDir =  DateTime.Now.Year.ToString() + "\\" + DateTime.Now.Month.ToString() + "\\"+ DateTime.Now.Day.ToString() + "\\";
            if (ImageFilePath.ToUpper().EndsWith(".PDF")) {
               
                
                DirectoryInfo BaseDir = new DirectoryInfo(BasePath);
                if (!BaseDir.Exists) {
                    return ("Error: Cannot Access " + BasePath);
                }
                else {
                    BaseDir.CreateSubdirectory(DocSaveDir);
                    FileInfo FileDone = new FileInfo(ImageFilePath);
                    FileDone.MoveTo((BasePath + (DocSaveDir + NewFileName)));
                    BaseDir.CreateSubdirectory(DateTime.Now.Year.ToString() + "\\" + DateTime.Now.Month.ToString() + "\\Checks");
                    // Create next month directories for storing images of checks
                    int NextMonth =     (DateTime.Now.Month +1);
                    if ((NextMonth == 13)) {
                        NextMonth = 1;
                        int NextYear = DateTime.Now.Year +1;
                        BaseDir.CreateSubdirectory(NextYear.ToString() + "\\" + NextMonth.ToString() + "\\Checks");
                         
                    }
                    else {
                        BaseDir.CreateSubdirectory(DateTime.Now.Year.ToString() + "\\" + NextMonth.ToString() + "\\" + "Checks");
                    }
                    FileDone = null;
                    BaseDir = null;
                }
            }
            deleteInputFileAndFolder(ImageFilePath);
            return (DocSaveDir + NewFileName);
        
        }


        public string deleteInputFileAndFolder(string DocInputPath)
        {
            // TODO: On Error GoTo Warning!!!: The statement is not translatable 
            string folderName = Path.GetDirectoryName(DocInputPath);

            try
            {
                Directory.Delete(folderName, true);
            }
            catch (Exception e)
            {
                Logger.log("Unable to delete folder: " + folderName + "\n " + e.Message.ToString());
                throw new IOException("Unable to delete folder: " + folderName);
            }

            return "Success";

        }
    }

}
