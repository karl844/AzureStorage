using AzureStorage.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.File.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AzureStorage.BLL
{
    public class AzureStorageHelper
    {
        private readonly CloudStorageAccount storageAccount;
        private readonly CloudFileClient fileClient;
        private CloudFileShare share;
        private CloudFileDirectory root;

        public AzureStorageHelper(string StorageConnectionString)
        {
            // Parse the connection string and return a reference to the storage account.   
            storageAccount = CloudStorageAccount.Parse(StorageConnectionString);

            // Create a CloudFileClient object for credentialed access to Azure Files.
            fileClient = storageAccount.CreateCloudFileClient();
        }

        /// <summary>
        /// Add a document to the azure fileshare
        /// </summary>                         
        /// <param name="folderName">The name of the folder used to store the file </param> 
        /// <param name="fileName">The name of the new file.</param> 
        /// <param name="file">The file to send to the server</param> 
        public async Task<Document> CheckFolderAndCreateAsync(string folderName, string fileName, IFormFile file)
        {            
            string _folderName = SetName(folderName);

            string _fileName = SetName(fileName);
            string newFileName = "";

            string year = string.Format("{0:yyyy}", DateTime.UtcNow);
            string month = string.Format("{0:MMM}", DateTime.UtcNow);
            string day = string.Format("{0:dd}", DateTime.UtcNow);

            string newPath = string.Format("{0}/{1}/{2}/{3}", _folderName, year, month, day);

            if (file != null)
            {
                await SetFileShareAsync();

                //Get reference to DIR of new path
                CloudFileDirectory directoryReference = root.GetDirectoryReference(newPath);

                //CREATE dir structure if not exist
                await CreateRecursiveIfNotExists(directoryReference);

                string fileExt = Path.GetExtension(file.FileName);
                int nameCnt = 0;

                //set new file name
                newFileName = _fileName + "_" + nameCnt++.ToString();

                //loop through DIR to see if file exists with same name if so , add cnt to rename.
                CloudFile azureFile = directoryReference.GetFileReference(newFileName + fileExt);

                while (await azureFile.ExistsAsync())
                {
                    newFileName = _fileName + "_" + nameCnt++.ToString();

                    azureFile = directoryReference.GetFileReference(newFileName + fileExt);
                }

                try
                {
                    await azureFile.UploadFromStreamAsync(file.OpenReadStream());

                    Document document = new Document
                    {
                        Directory = newPath,
                        FileShare = share.Name,
                        Created = DateTime.UtcNow,
                        Name = newFileName + fileExt,
                        Size = (int)file.Length
                    };

                    return document;
                }
                catch (Exception)
                {                    
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Delete file from azure cloud
        /// </summary>
        /// <param name="directory">Directory from Document Table</param>
        /// <param name="documentName">Document name from Document Table</param>
        /// <returns></returns>
        public async Task<bool> DeleteDocumentAsync(Document document)
        {
            share = fileClient.GetShareReference(document.FileShare);

            root = share.GetRootDirectoryReference();

            CloudFileDirectory directoryReference = root.GetDirectoryReference(document.Directory);

            CloudFile cloudFile = directoryReference.GetFileReference(document.Name);

            return await cloudFile.DeleteIfExistsAsync();
        }

        /// <summary>
        /// Create the directory
        /// </summary>
        /// <param name="directory">Path if DIR to create</param>        
        private async Task CreateRecursiveIfNotExists(CloudFileDirectory directory)
        {
            if (!await directory.ExistsAsync())
            {
                await CreateRecursiveIfNotExists(directory.Parent);
                await directory.CreateAsync();
            }
        }

        /// <summary>
        /// Get & Set the file share in use. File shares have 5TB limit. if fileshare1 is full create new one, if fileshare2 is full create a new one etc.
        /// </summary>
        private async Task SetFileShareAsync()
        {
            try
            {
                //File share names can contain only lowercase letters, numbers, and hyphens, and must begin and end with a letter or a number. 
                //The name cannot contain two consecutive hyphens.
                string fileShare = "fileshare";
                int fileShareNo = 1;

                //Create a share for organizing files and directories within the storage account.           
                share = fileClient.GetShareReference(fileShare + string.Format("{0:00}", fileShareNo));

                while (!await CheckFileShareCreateAsync(share))
                {
                    share = fileClient.GetShareReference(fileShare + string.Format("{0:00}", fileShareNo++));
                }

                //Get a reference to the root directory of the share.        
                root = share.GetRootDirectoryReference();
            }
            catch (StorageException)
            {
                throw;
            }
        }

        /// <summary>
        /// Check if file share exists and is less than 5TB, if not create a new one
        /// </summary>
        /// <param name="cloudShare"></param>
        private async Task<bool> CheckFileShareCreateAsync(CloudFileShare cloudShare)
        {
            if (!await cloudShare.ExistsAsync())
            {
                await cloudShare.CreateIfNotExistsAsync();
                return true;
            }

            ShareStats stats = await cloudShare.GetStatsAsync();
            if (stats.Usage > 4999)
            {
                return false;
            }

            return true;
        }

        private string SetName(string _name)
        {
            string name = _name;

            name = name.Replace(" ", "_");
            name = name.Replace("/", "_");
            name = name.Replace("\\", "_");          

            return name;
        }

        /// <summary>
        /// Get file from the Azure Storage 
        /// </summary>
        /// <param name="document">The document from the document table to be downloaded</param>
        public CloudFile GetFileAsync(Document document)
        {
            //set the share & the root DIR
            share = fileClient.GetShareReference(document.FileShare);
            root = share.GetRootDirectoryReference();

            //set the directoryReference of the documents directory
            CloudFileDirectory directoryReference = root.GetDirectoryReference(document.Directory);

            //return the reference for the file to be downloaded
            CloudFile azureFile = directoryReference.GetFileReference(document.Name);

            return azureFile;
        }

    }
}
