using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;

using xmlfilevalidator;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Util;
using Amazon.S3.Model;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.Extensions.Configuration;

namespace xmlfilevalidator.Tests
{
    public class FunctionTest
    {
        IConfiguration Configuration { get; set; }

        [Fact]
        public async Task IntegrationTest()
        {

            
            var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            Configuration = builder.Build();

            var options = Configuration.GetAWSOptions();
            IAmazonS3 s3client = options.CreateServiceClient<IAmazonS3>();
            //IAmazonS3 s3client = new AmazonS3Client(RegionEndpoint.USEast1);
            var bucketName = "siri-lambda-test";
            var schemaName = "books.xsd";
            var checktype = "False";
            var fileNameGood = "booksSchemaPass.xml";
            var fileNameFail = "booksSchemaFail.xml";
            var s3Event = new S3Event
            {
                Records = new List<S3EventNotification.S3EventNotificationRecord>
                    {
                        new S3EventNotification.S3EventNotificationRecord
                        {
                            S3 = new S3EventNotification.S3Entity
                            {
                                Bucket = new S3EventNotification.S3BucketEntity {Name = "siri-lambda-test" },
                                Object = new S3EventNotification.S3ObjectEntity {Key = fileNameGood }
                            }
                        }
                    }
            };

            var function = new Function(s3client, bucketName, schemaName);
            var context = new TestLambdaContext();
            var result = await function.FunctionHandler(s3Event, context);
                


            Assert.Equal("good", result);
        }
    }
}
