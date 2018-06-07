using System;
using System.Collections.Generic;

namespace CPubLib
{
    public class Metadata
    {
        public const string DefaultLanguage = "en-us";

        public string ID { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; }
        public string Author { get; set; }
        public string Publisher { get; set; }
        public DateTime PublishingDate { get; set; } = DateTime.UtcNow;
        public string Language { get; set; } = DefaultLanguage;

        public string Description { get; set; }
        public ISet<string> Tags { get; } = new SortedSet<string>();
        public string Source { get; set; }
        public string Relation { get; set; }
        public string Copyright { get; set; }

        public bool Valid => Validate();

        private bool Validate()
        {
            if (!ValidateProperty(ID))
                return false;

            if (!ValidateProperty(Title))
                return false;

            if (!ValidateProperty(Author))
                return false;

            if (!ValidateProperty(Publisher))
                return false;

            if (!ValidateProperty(Language))
                return false;

            return true;
        }

        private bool ValidateProperty(string property)
        {
            if (string.IsNullOrEmpty(property) || string.IsNullOrWhiteSpace(property))
            {
                return false;
            }

            return true;
        }
    }
}
