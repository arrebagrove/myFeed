using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Windows.Storage;

namespace myFeed
{
    public static class SerializerExtensions
    {
        public static async void SerializeObject<T>(T serializableObject, StorageFile file)
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

                    using (var stringWriter = new StringWriter())
                    using (var xmlTextWriter = XmlWriter.Create(stringWriter))
                    {
                        xmlDocument.WriteTo(xmlTextWriter);
                        xmlTextWriter.Flush();
                        string finish = stringWriter.GetStringBuilder().ToString();
                        await FileIO.WriteTextAsync(file, finish);
                    }

                    stream.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex.ToString());
            }
        }

        public static async Task<T> DeSerializeObject<T>(StorageFile file)
        {
            string filestring = await FileIO.ReadTextAsync(file);
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
            catch (Exception ex)
            {
                Debug.Write(ex.ToString());
            }
            return objectOut;
        }
    }
}
