using DinkToPdf.Contracts;
using DinkToPdf;

namespace IgnisEducationSuite.ServerServices
{
    public class PDFService
    {
        private readonly IConverter _converter;
        public PDFService(IConverter converter)
        {
            _converter = converter;
        }
        public byte[] GeneratePdf(string htmlContent)
        {
            try
            {
                var doc = new HtmlToPdfDocument()
                {
                    GlobalSettings = { PaperSize = PaperKind.A4, Orientation = Orientation.Portrait, },
                    Objects = { new ObjectSettings() { HtmlContent = htmlContent, } }
                };
                return _converter.Convert(doc);
            }
            catch (Exception ex)
            {
                var _ = ex.Message;
                throw;
            }
           
        }
    }
}
