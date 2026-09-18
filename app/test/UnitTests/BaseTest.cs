using Microsoft.Extensions.Configuration;
using Models.Event;
using NSubstitute;
using ServiceClients.Navitus;
using Services.Interface;
using Shared.Services.Auth;

namespace UnitTests
{
    public class BaseTests
    {
        public IUMServiceClient mockUMServiceClient;
        public IAuthProvider mockAuthProvider;
        public IConfiguration configuration;
        public ITemplatedNoteService mockTemplatedNoteService;

        public BaseTests()
        {
            Environment.SetEnvironmentVariable("NavitusUtilizationManagementBaseURL", "test-url");

            mockUMServiceClient = Substitute.For<IUMServiceClient>();
            mockAuthProvider = Substitute.For<IAuthProvider>();
            configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            mockTemplatedNoteService = Substitute.For<ITemplatedNoteService>();
        }

        public static EpisodeBase GetEpisodeBase()
        {
            return new EpisodeBase
            {
                NavitusEpisodeId = 1,
                MemberInformation = new MemberInformation()
                {
                    PersonCode = "01",
                    SubscriberNumber = "123456789*Test-CarrierId",
                    GroupNumber = "02",
                    UniqueId = "123456789010"
                },
                CarrierId = "Test-CarrierId",
                ExternalEpisodeId = "Transaction-123",
                DrugIdentifiers = new EpisodeDrugIdentifiers() { Gpi = "12345678910", Ndc = "12345678910123" },
                PrescriberId = "Test-PRESCRIBER-ID",
                EpisodeSourceType = EpisodeSourceType.Fax,
                InitiationDateTime = new DateTime(2000, 1, 1),
                FinalizationDateTime = new DateTime(2000, 2, 2, 0, 0, 0, DateTimeKind.Utc),
                MandatorySpecialtyPharmacy = "Test-MandatorySpecialtyPharmacy",
                ApprovalDuration = "8 months",
                RPhReviewerId = "Test-RPhReviewerId",
                MDReviewerId = "Test-MDReviewerId",
                NavitusAuthorizationNumber = 123,
                Urgent = true,
                PriorAuthStatus = PriorAuthStatus.InProgress,
                DecisionLanguage = "Test-DecisionLanguage",
                DrugDescription = "Test-DrugDescription",
                CoverageLevel = "Test-CoverageLevel",
                QuantityLimit = "Test-QuantityLimit",
                Reason = "Test-Reason",
                DenialReason = "Test-DenialReason",
                ModifiedBy = "Test-ModifiedBy"
            };
        }

        public static EpisodeBase GetGrievanceAndAppealEpisodeBase()
        {
            return new EpisodeBase
            {
                NavitusEpisodeId = 2,
                MemberInformation = new MemberInformation()
                {
                    PersonCode = "01",
                    SubscriberNumber = "123456789*Test-CarrierId",
                    GroupNumber = "02",
                    UniqueId = "123456789010"
                },
                CarrierId = "Test-CarrierId",
                ExternalEpisodeId = "Transaction-123",
                DrugIdentifiers = new EpisodeDrugIdentifiers() { Gpi = "12345678910", Ndc = "12345678910123" },
                PrescriberId = "Test-PRESCRIBER-ID",
                EpisodeSourceType = EpisodeSourceType.Fax,
                InitiationDateTime = new DateTime(2000, 1, 1),
                FinalizationDateTime = new DateTime(2000, 2, 2, 0, 0, 0, DateTimeKind.Utc),
                MandatorySpecialtyPharmacy = "Test-MandatorySpecialtyPharmacy",
                ApprovalDuration = "8 months",
                RPhReviewerId = "Test-RPhReviewerId",
                MDReviewerId = "Test-MDReviewerId",
                NavitusAuthorizationNumber = 123,
                Urgent = true,
                DecisionLanguage = "Test-DecisionLanguage",
                DrugDescription = "Test-DrugDescription",
                CoverageLevel = "Test-CoverageLevel",
                QuantityLimit = "Test-QuantityLimit",
                Reason = "Test-Reason",
                DenialReason = "Test-DenialReason",
                ModifiedBy = "Test-ModifiedBy",
                ExternalRelatedPriorAuthEpisodeId = "RelatedTran-123",
                NavitusRelatedPriorAuthEpisodeId = 1,
                GrievanceAndAppealsCaseType = GrievanceAndAppealsCaseType.Grievance,
                GrievanceAndAppealsStatus = GrievanceAndAppealsStatus.Approved
            };
        }
    }
}
