using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Oracle.ManagedDataAccess.Client;
using System.Configuration;
using System.Diagnostics;


namespace SaveDocumentsToGMS
{
    class Program
    {
        static OracleConnection connection;
        static DBUtil dbUtil = null;
        static string fileTemplate = null;
        static string docType = "A";    // A or P , Accounting or Plan

        static public int Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                Console.WriteLine("Error, need document type A(ccounting) or P(lan)");
                return 8;
            }
            else
            {
                docType = args[0].ToUpper();
            }

            //Logger.filePath = @"c:\Processing_log.txt";
            Logger.filePath = @ConfigurationManager.AppSettings["logPath"];

            Logger.flush();
            Console.WriteLine("Getting DB Connection");
            try
            {
                dbUtil = new DBUtil(ConfigurationManager.AppSettings["dbUser"],
                    ConfigurationManager.AppSettings["dbPassword"],
                    ConfigurationManager.AppSettings["host"],
                    ConfigurationManager.AppSettings["database"]);


            }
            catch (Exception e)
            {
                Console.WriteLine("Error, {0}\n {1}", e.GetType(), e.Message.ToString());

            }


            try
            {
                connection = dbUtil.getConnection();


            }
            catch (Exception ex)
            {
                Console.WriteLine("Error 99, {0}\n {1}", ex.GetType(), ex.Message.ToString());
                Logger.log("Error 99," + ex.GetType() + "\t" + ex.Message.ToString());

            }
            if (docType == "A")
            {
                fileTemplate = ConfigurationManager.AppSettings["AnnualAccounting_Template"];
                readFolder(@ConfigurationManager.AppSettings["accountingFolder"]);
                return 0;
            }
            else if (docType == "P")
            {
                fileTemplate = ConfigurationManager.AppSettings["AnnualPlan_Template"];
                readFolder(@ConfigurationManager.AppSettings["planFolder"]);
                return 0;
            }
            return 0;
        }



        static string readFolder(string InputFolder)
        {
            string[] fileEntries = Directory.GetFiles(InputFolder, "*.pdf", SearchOption.AllDirectories);
            Console.WriteLine("reading folder {0}", InputFolder);
           // Logger.log("Reading folder " + InputFolder);
            Int16 cnt = 0;
            int rc = 0;

            string sourceFileName = null;
            foreach (string fileName in fileEntries)
            {
                cnt++;
                sourceFileName = makeFileName(fileName);

                if (sourceFileName.Substring(0, 5) != "ERROR")
                {
                    Console.WriteLine("Processing file# {0} - {1}", cnt, sourceFileName);
                    Logger.log("Processing file: " + cnt + " - " + sourceFileName);
                }
                else
                {
                    Logger.log("Unable to process file: " + cnt + " - " + fileName);
                }

            }
            dbUtil.dropConnection(connection);
            return "OK";
        }

        public static System.Boolean IsNumeric(System.Object Expression)
        {
            if (Expression == null || Expression is DateTime)
                return false;

            if (Expression is Int16 || Expression is Int32 || Expression is Int64 || Expression is Decimal || Expression is Single || Expression is Double || Expression is Boolean)
                return true;

            try
            {
                if (Expression is string)
                    Double.Parse(Expression as string);
                else
                    Double.Parse(Expression.ToString());
                return true;
            }
            catch { } // just dismiss errors but return false
            return false;
        }



        static string makeFileName(string path)
        {

            string[] words;
            string fileName = null;
            string sourcePath = path;
            // for Annual Accounting: WARD-RETPRP-0-214-SEQ.pdf ; 
            // for Annual Plan WARD-ANPLAN-0-215-SEQ.pdf;
            string fileBody = fileTemplate;  // file name template for permanent storage in the ScannedDocs\WardDocuments folder
            string fileNameLessWard = "_AnnualAccounting.pdf";
            string wardNumber = null;
            decimal seq = 0;
            int rc;
            char[] charSeparators = new[] { '_' };

            fileName = Path.GetFileName(path);  /*  9999_AnnualAccounting.pdf is the expected format of the souce document*/
            words = fileName.Split(charSeparators,2);
            wardNumber = words[0];


            bool allDigits = wardNumber.All(char.IsDigit);
            if (docType == "A")
            {
                fileNameLessWard = "_" + words[1]; ;
            }
            if (docType == "P")
            {
                fileNameLessWard = "_"+ words[1];
            }
            // if (allDigits && fileName == wardNumber + "_AnnualAccounting.pdf")
            if (allDigits && fileName == wardNumber + fileNameLessWard)
            {
                seq = dbUtil.getNextSequence(connection);
                fileName.Replace("WARD", words[0]);
                fileBody = fileBody.Replace("WARD", words[0]);
                fileBody = fileBody.Replace("SEQ", seq.ToString());
                if (connection.State.ToString() == "Open")
                {
                    switch (docType)
                    {
                        case "A":
                            rc = dbUtil.saveAnnualAccountingToGMS(path, seq, decimal.Parse(words[0]), fileBody, connection);
                            break;
                        case "P":
                            rc = dbUtil.saveAnnualPlanToGMS(path, seq, decimal.Parse(words[0]), fileBody, connection);
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("connection is closed");
                }
                return fileBody;
            }
            else
            {
                Console.WriteLine("filename error: {0}", path);
                return "ERROR =>";
            }
        }


        static string copyDocuments(string targetDirectory)
        {


            string fileName = null;
            string sourcePath = null; // @"z:\WardDocuments";
            string targetPath = targetDirectory;
            string workingTargetPath = targetPath;
            string workingSourcePath = sourcePath;
            //string[] folders;

            try
            {
                System.IO.Directory.Delete(targetPath, true);       // remove any files from previous run
            }
            catch (IOException)
            {
                // Console.WriteLine("Target directory {0} does not Exist will Create", targetPath);
                Console.Write("");
            }
            try
            {
                for (int i = 0; i < 1; i++)          // process all of the wards documents
                {

                    workingSourcePath = sourcePath;

                    for (int j = 0; j < 1; j++)
                    {
                        workingTargetPath += "\\h";           // build input and output path
                        workingSourcePath += "\\hh";
                    }


                    string sourceFile = System.IO.Path.Combine(workingSourcePath, fileName);   // complete path info
                    string destFile = System.IO.Path.Combine(targetPath, fileName);
                    if (!File.Exists(sourceFile))
                    {
                        Console.WriteLine("LOG: => Bank Statement not found in filesystem for ward {0}", 122);

                        System.IO.File.AppendAllText(@"P:\Annuals\log.txt", "Ward: " + 122 + " Bank Statement not found in filesystem! " + DateTime.Now + "\r\n");
                    }
                    // targetPath = targetPath + doc.getWardName() + "_" + doc.getWard();     // this tells us where to store the Annual Accounting for this ward
                    Console.WriteLine("file {0}:  {1} ", i + 1, destFile);                        // list the files as they are being processed
                    if (!System.IO.Directory.Exists(targetPath))                         // create output directory as needed for each document
                    {
                        System.IO.Directory.CreateDirectory(targetPath);
                    }
                    if (File.Exists(sourceFile))
                    {
                        System.IO.File.Copy(sourceFile, destFile, true);                        // actually copy the file
                    }
                    else
                    {
                        Console.WriteLine("Missing File: {0}", sourceFile);                // if missing file alert and continue
                    }
                    workingTargetPath = targetPath;                                         // reset 
                    workingSourcePath = "";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading Documents. {0}", e.ToString());
                return "Error reading Documents " + e.ToString();
            }

            return targetPath;
        }                   // end of function copyDocuments


    }
}
