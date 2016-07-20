using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Windows.Storage;

namespace myFeed
{
    public static class SerializerExtensions
    {
        public static async void SerializeObject<T>(T serializableObject, string fileName)
        {
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                XmlSerializer serializer = new XmlSerializer(serializableObject.GetType());
                using (MemoryStream stream = new MemoryStream())
                {
                    serializer.Serialize(stream, serializableObject);
                    stream.Position = 0;
                    xmlDocument.Load(stream);

                    StorageFolder storageFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("favorites");

                    using (var stringWriter = new StringWriter())
                    using (var xmlTextWriter = XmlWriter.Create(stringWriter))
                    {
                        xmlDocument.WriteTo(xmlTextWriter);
                        xmlTextWriter.Flush();
                        string finish = stringWriter.GetStringBuilder().ToString();
                        await FileIO.WriteTextAsync(await storageFolder.CreateFileAsync(fileName), finish);
                    }

                    stream.Dispose();
                }
            }
            catch
            {
                //Log exception
            }
        }

        public static T DeSerializeObject<T>(string filestring)
        {
            T objectOut = default(T);
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (TextReader reader = new StringReader(filestring))
                {
                    objectOut = (T)serializer.Deserialize(reader);
                    reader.Dispose();
                }
            }
            catch
            {
                //Log exception
            }
            return objectOut;
        }
    }
}
