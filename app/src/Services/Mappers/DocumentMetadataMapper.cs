using Models.Event;
using ServiceClients.Navitus;
using ServiceClients.Navitus.Document;

namespace Services.Mappers
{
    public static class DocumentMetadataMapper
    {
        /// <summary>
        /// Maps document(mail) received event to a CreateDocumentMetadataRequest
        /// </summary>
        /// <param name="@event"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static CreateDocumentMetadataRequest MapToCreateDocumentMetadataRequest(this OutboundMailReceived @event)
        {
            var result = new CreateDocumentMetadataRequest()
            {
                DocumentId = @event.S3ObjectKey,
                DocumentType = DocumentType.Mail,
                ExternalEpisodeId = @event.ExternalEpisodeId,
                NavitusEpisodeId = long.Parse(@event.NavitusEpisodeId),
                ReceivedUTC = DateTimeOffset.UtcNow, // TODO: Get the real time when the file was downloaded
                RecipientName = @event.RecipientName,
                TransactionId = @event.TransactionId,
                DocumentRecipient = @event.DocumentRecipient,
                LineOfBusiness = @event.LineOfBusiness,
                CarrierId = @event.CarrierId,
                Outcome = @event.Outcome
            };

            return result;
        }

        /// <summary>
        /// Maps document(fax) received event to a CreateDocumentMetadataRequest
        /// </summary>
        /// <param name="@event"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static CreateDocumentMetadataRequest MapToCreateDocumentMetadataRequest(this OutboundFaxReceived @event)
        {
            var result = new CreateDocumentMetadataRequest()
            {
                DocumentId = @event.S3ObjectKey,
                DocumentType = DocumentType.Fax,
                ExternalEpisodeId = @event.ExternalEpisodeId,
                NavitusEpisodeId = long.Parse(@event.NavitusEpisodeId),
                ReceivedUTC = DateTimeOffset.UtcNow, // TODO: Get the real time when the file was downloaded
                RecipientName = @event.RecipientName,
                TransactionId = @event.TransactionId,
                FaxNumber = @event.FaxNumber
            };

            return result;
        }
    }
}
