using System;
using System.ComponentModel.DataAnnotations;

namespace Registration.ReadModel {
    public class Conference {
        [Key]
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string Tagline { get; set; }
        public string TwitterSearch { get; set; }
        public DateTimeOffset StartDate { get; set; }

        public bool IsPublished { get; set; }

        protected Conference() { }

        public Conference(Guid id, string code, string name, string description, string location, string tagline, string twitterSearch, DateTimeOffset startDate) {
            this.Id = id;
            this.Code = code;
            this.Name = name;
            this.Description = description;
            this.Location = location;
            this.Tagline = tagline;
            this.TwitterSearch = twitterSearch;
            this.StartDate = startDate;
        }
    }
}