using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Event
{
    public class OutboundFaxReceived
    {
        /// <summary>
        /// Navitus episode Id associated with the outbound fax
        /// </summary>
        public string NavitusEpisodeId { get; set; } = default!;

        /// <summary>
        /// External episode Id associated with the outbound fax
        /// </summary>
        public string ExternalEpisodeId { get; set; } = default!;

        /// <summary>
        /// Unique identifier for the current outbound mail transaction. Should be used for correlation between systems.
        /// </summary>
        public string TransactionId { get; set; } = default!;

        /// <summary>
        /// Fax number to send the fax to
        /// </summary>
        public string FaxNumber { get; set; }

        /// <summary>
        /// Name of the Recipient
        /// </summary>
        public string RecipientName { get; set; } = default!;

        /// <summary>
        /// Intended Recipient of document ex:Prescriber, Member, and Pharmacy
        /// </summary>
        public string DocumentRecipient { get; set; }

        /// <summary>
        /// Hash of the pdf file to verify that the contents of a file have not been changed
        /// </summary>
        public string FileHash { get; set; } = default!;

        /// <summary>
        /// Hashing algorithm that is used to generate the FileHash
        /// </summary>
        public HashingAlgorithmType HashingAlgorithm { get; set; }

        /// <summary>
        /// S3 Object Key
        /// </summary>
        public string S3ObjectKey { get; set; } = default!;
    }
}
