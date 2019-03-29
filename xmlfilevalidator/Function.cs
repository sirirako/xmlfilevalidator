using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace xmlfilevalidator
{
    public class Function
    {

        IAmazonS3 S3Client { get; set; }

        public string stxsd { get; set; }
        public string Schema_Target { get; set; }
        public string SchemaName { get; set; }
        public string BucketName { get; set; }

        public Function()
        {
            S3Client = new AmazonS3Client();
            var buckName = System.Environment.GetEnvironmentVariable("BUCKET_NAME");
            var schemaName = System.Environment.GetEnvironmentVariable("SCHEMA_FILENAME");
            var schema_Target = System.Environment.GetEnvironmentVariable("SCHEMA");
            BucketName = string.IsNullOrEmpty(buckName) ? "siri-lambda-test" : buckName;
            SchemaName = string.IsNullOrEmpty(schemaName) ? "books.xsd" : schemaName;
            Schema_Target = string.IsNullOrEmpty(schema_Target) ? "urn:books" : schema_Target;
            stxsd = GetObject(BucketName, SchemaName).Result;
        }

        public Function(IAmazonS3 s3Client, string bucketName, string schemaName)
        {
            this.S3Client = s3Client;
            stxsd = GetObject(bucketName, schemaName).Result;
        }

        public async Task<string> FunctionHandler(S3Event evnt, ILambdaContext context)
        {
            var s3Event = evnt.Records?[0].S3;
            if (s3Event == null)
            {
                return null;
            }
            string curFile = "/tmp/schema.xsd";
            System.IO.File.WriteAllText(curFile, stxsd);
            var response = await S3Client.GetObjectAsync(s3Event.Bucket.Name, s3Event.Object.Key);
            using (var filereader = new StreamReader(response.ResponseStream))
            {
                string s3object = await filereader.ReadToEndAsync();
                XmlReaderSettings settings = new XmlReaderSettings();
                byte[] byteArray = Encoding.ASCII.GetBytes(s3object);
                MemoryStream stream = new MemoryStream(byteArray);
                XmlReader xmlReaderS3Object = XmlReader.Create(stream);
                context.Logger.LogLine("Validating " + s3Event.Object.Key);
                context.Logger.LogLine("Schema File Name: " + SchemaName);
                settings.Schemas.Add(Schema_Target, curFile);
                settings.CheckCharacters = true;
                settings.ValidationType = ValidationType.Schema;
                //settings.ValidationEventHandler += new ValidationEventHandler(booksSettingsValidationEventHandler);
                try
                {
                    XmlReader reader = XmlReader.Create(xmlReaderS3Object, settings);
                    while (reader.Read()) { }
                    return "good";
                }
                catch (Exception e)
                {
                    context.Logger.LogLine(e.Message);
                    return "bad";
                }
            } 
        }

        public async Task<string> GetObject(string bucket, string key)
        {
            var response = await S3Client.GetObjectAsync(bucket, key);
            using (var reader = new StreamReader(response.ResponseStream))
            {
                string s3object = await reader.ReadToEndAsync();
                return s3object;
            }
        }

        static void booksSettingsValidationEventHandler(object sender, ValidationEventArgs e)
        {
            if (e.Severity == XmlSeverityType.Warning)
            {
                
                Console.Write("WARNING: ");
                Console.WriteLine(e.Message);
            }
            else if (e.Severity == XmlSeverityType.Error)
            {
                Console.Write("ERROR: ");
                Console.WriteLine(e.Message);
            }
        }
    }
}
