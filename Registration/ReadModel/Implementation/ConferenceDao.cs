using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Registration.ReadModel.Implementation {
    public class ConferenceDao: IConferenceDao {
        private readonly Func<ConferenceRegistrationDbContext> _contextFactory;
        public ConferenceDao(Func<ConferenceRegistrationDbContext> contextFactory) {
            this._contextFactory = contextFactory;
        }

        public ConferenceDetails GetConferenceDetails(string conferenceCode) {
            using(var context = this._contextFactory.Invoke()) {
                return context.Query<Conference>()
                    .Where(dto => dto.Code == conferenceCode)
                    .Select(x => new ConferenceDetails() {
                        Id = x.Id,
                        Code = x.Code,
                        Name = x.Name,
                        Description = x.Description,
                        Location = x.Location,
                        Tagline = x.Tagline,
                        TwitterSearch = x.TwitterSearch,
                        StartDate = x.StartDate
                    }).FirstOrDefault();
            }
        }

        public ConferenceAlias GetConferenceAlias(string conferenceCode) {
            using(var context = this._contextFactory.Invoke()) {
                return context.Query<Conference>()
                    .Where(dto => dto.Code == conferenceCode)
                    .Select(x => new ConferenceAlias() {Id = x.Id, Code = x.Code, Name = x.Name, Tagline = x.Tagline})
                    .FirstOrDefault();
            }
        }

        public IList<ConferenceAlias> GetPublishedConferences() {
            using (var context = this._contextFactory.Invoke()) {
                return context.Query<Conference>()
                    .Where(dto => dto.IsPublished)
                    .Select(x => new ConferenceAlias() { Id = x.Id, Code = x.Code, Name = x.Name, Tagline = x.Tagline })
                    .ToList();
            }
        }

        public IList<SeatType> GetPublishedSeatTypes(Guid conferenceId) {
            using (var context = this._contextFactory.Invoke()) {
                return context.Query<SeatType>()
                    .Where(dto => dto.ConferenceId == conferenceId)
                    .ToList();
            }
        }

        public IList<SeatTypeName> GetSeatTypeNames(IEnumerable<Guid> seatTypes) {
            var distinctIds = seatTypes.Distinct().ToArray();
            if(distinctIds.Length==0) {
                return new List<SeatTypeName>();
            }

            using (var context = this._contextFactory.Invoke()) {
                return context.Query<SeatType>()
                    .Where(dto => distinctIds.Contains(dto.Id))
                    .Select(s=>new SeatTypeName{Id = s.Id, Name = s.Name})
                    .ToList();
            }
        }
    }
}