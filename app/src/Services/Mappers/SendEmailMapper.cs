using Models.Event;
using Microsoft.Graph.Models;

namespace Services.Mappers
{
    public static class SendEmailMapper
    {
        /// <summary>
        /// Maps SendEmail event data to a Graph Message
        /// </summary>
        /// <param name="sendEmailEvent"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Message MapToGraphMessage(this SendEmail sendEmailEvent, string fromAddress)
        {
            var message = new Message
            {
                Subject = sendEmailEvent.Subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = sendEmailEvent.HtmlBody
                },
                ToRecipients = sendEmailEvent.ToEmailaddress?
                    .Select(email => new Recipient
                    {
                        EmailAddress = new EmailAddress { Address = email }
                    })
                    .ToList(),
                CcRecipients = sendEmailEvent.CcEmailaddress?
                    .Select(email => new Recipient
                    {
                        EmailAddress = new EmailAddress { Address = email }
                    })
                    .ToList(),
                BccRecipients = sendEmailEvent.BccEmailaddress?
                    .Select(email => new Recipient
                    {
                        EmailAddress = new EmailAddress { Address = email }
                    })
                    .ToList(),
                From = new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = fromAddress 
                    }
                }
            };

            message.SingleValueExtendedProperties = new List<SingleValueLegacyExtendedProperty>()
            {
                new SingleValueLegacyExtendedProperty
                {
                    // see https://learn.microsoft.com/en-us/office/client-developer/outlook/mapi/pidtagsensitivity-canonical-property
                    Id = "Integer 0x0036",
                    Value = sendEmailEvent.Sensitivity.ToString()
                }
            }; 

            return message;
        }
    }
}
