namespace Shared.Authorization
{
    public static class Scopes
    {
        public static class Client
        {
            public const string NucleusNavitus = "nucleus/navitus";
        }

        public static class PriorAuthEpisode
        {
            public const string Read = "priorauth.episode/read";
            public const string Write = "priorauth.episode/write";
        }

        public static class PriorAuthDocument
        {
            public const string Read = "priorauth.document/read";
            public const string Write = "priorauth.document/write";
        }
    }
}
