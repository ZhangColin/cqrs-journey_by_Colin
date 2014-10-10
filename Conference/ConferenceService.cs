using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Linq;
using Conference.Contracts;
using Infrastructure.Messaging;

namespace Conference {
    /// <summary>
    /// 会议服务
    /// </summary>
    public class ConferenceService {
        private readonly IEventBus _eventBus;
        private readonly string _nameOrConnectionString;

        public ConferenceService(IEventBus eventBus, string nameOrConnectionString = "ConferenceManagement") {
            this._eventBus = eventBus;
            this._nameOrConnectionString = nameOrConnectionString;
        }

        /// <summary>
        /// 创建会议
        /// </summary>
        /// <param name="conference"></param>
        public void CreateConference(ConferenceInfo conference) {
            using(ConferenceContext context = new ConferenceContext(this._nameOrConnectionString)) {
                bool existingSlug = context.Conferences.Where(c=>c.Slug==conference.Slug)
                    .Select(c=>c.Slug).Any();

                if(existingSlug) {
                    throw new DuplicateNameException("The chosen conference slug is already taken.");    
                }

                if(conference.IsPublished) {
                    conference.IsPublished = false;
                }

                context.Conferences.Add(conference);

                context.SaveChanges();

                this.PublishConferenceEvent<ConferenceCreated>(conference);
            }
        }

        /// <summary>
        /// 创建座位
        /// </summary>
        /// <param name="conferenceId"></param>
        /// <param name="seat"></param>
        public void CreateSeat(Guid conferenceId, SeatType seat) {
            using (ConferenceContext context = new ConferenceContext(this._nameOrConnectionString)) {
                var conference = context.Conferences.Find(conferenceId);

                if(conference==null) {
                    throw new ObjectNotFoundException();
                }

                conference.Seats.Add(seat);

                context.SaveChanges();

                if(conference.WasEverPublished) {
                    this.PublishSeatCreated(conferenceId, seat);
                }
            }
        }

        public ConferenceInfo FindConference(string slug) {
            using (ConferenceContext context = new ConferenceContext(this._nameOrConnectionString)) {
                return context.Conferences.FirstOrDefault(x => x.Slug==slug);
            }
        }

        public ConferenceInfo FindConference(string email, string accessCode) {
            using (ConferenceContext context = new ConferenceContext(this._nameOrConnectionString)) {
                return context.Conferences.FirstOrDefault(x => x.OwnerEmail == email && x.AccessCode == accessCode);
            }
        }

        public IEnumerable<SeatType> FindSeatTypes(Guid conferenceId) {
            using (ConferenceContext context = new ConferenceContext(this._nameOrConnectionString)) {
                return context.Conferences.Include(x => x.Seats)
                    .Where(x => x.Id == conferenceId)
                    .Select(x => x.Seats)
                    .FirstOrDefault() ?? Enumerable.Empty<SeatType>();
            }
        } 

        public SeatType FindSeatType(Guid seatTypeId) {
            using (ConferenceContext context = new ConferenceContext(this._nameOrConnectionString)) {
                return context.Seats.Find(seatTypeId);
            }
        }

        public IEnumerable<Order> FindOrders(Guid conferenceId) {
            using(ConferenceContext context = new ConferenceContext(this._nameOrConnectionString)) {
                return context.Orders.Include("Seats.SeatInfo").Where(x => x.ConferenceId == conferenceId).ToList();
            }
        } 

        public void UpdateConference(ConferenceInfo conference) {
            using(ConferenceContext context = new ConferenceContext(this._nameOrConnectionString)) {
                var existing = context.Conferences.Find(conference.Id);

                if(existing==null) {
                    throw new ObjectNotFoundException();
                }

                context.Entry(existing).CurrentValues.SetValues(conference);
                context.SaveChanges();

                this.PublishConferenceEvent<ConferenceUpdated>(conference);
            }
        }

        public void UpdateSeat(Guid conferenceId, SeatType seat) {
            using(ConferenceContext context = new ConferenceContext(this._nameOrConnectionString)) {
                SeatType existing = context.Seats.Find(seat.Id);

                if(existing==null) {
                    throw new ObjectNotFoundException();
                }

                context.Entry(existing).CurrentValues.SetValues(seat);

                context.SaveChanges();

                if(context.Conferences.Where(x=>x.Id==conferenceId).Select(x=>x.WasEverPublished).FirstOrDefault()) {
                    this._eventBus.Publish(new SeatUpdated() {
                        ConferenceId = conferenceId,
                        SourceId = seat.Id,
                        Name = seat.Name,
                        Description = seat.Description,
                        Price = seat.Price,
                        Quantity = seat.Quantity
                    });
                }
            }
        }

        public void Publish(Guid conferenceId) {
            this.UpdatePublished(conferenceId, true);
        }

        public void Unpublish(Guid conferenceId) {
            this.UpdatePublished(conferenceId, false);
        }

        private void UpdatePublished(Guid conferenceId, bool isPublished) {
            using(ConferenceContext context = new ConferenceContext(this._nameOrConnectionString)) {
                ConferenceInfo conference = context.Conferences.Find(conferenceId);
                if(conference==null) {
                    throw new ObjectNotFoundException();
                }

                conference.IsPublished = isPublished;
                if(isPublished && !conference.WasEverPublished) {
                    conference.WasEverPublished = true;
                    context.SaveChanges();

                    foreach(SeatType seat in conference.Seats) {
                        this.PublishSeatCreated(conference.Id, seat);
                    }
                }
                else {
                    context.SaveChanges();
                }

                if(isPublished) {
                    this._eventBus.Publish(new ConferencePublished(){SourceId = conferenceId});
                }
                else {
                    this._eventBus.Publish(new ConferenceUnpublished(){SourceId = conferenceId});
                }
            }
        }

        public void DeleteSeat(Guid id) {
            using(ConferenceContext context = new ConferenceContext(this._nameOrConnectionString)) {
                SeatType seat = context.Seats.Find(id);
                if(seat==null) {
                    throw new ObjectNotFoundException();
                }

                bool wasPublished = context.Conferences.Where(x => x.Seats.Any(s => s.Id == id))
                    .Select(x => x.WasEverPublished)
                    .FirstOrDefault();

                if(wasPublished) {
                    throw new InvalidOperationException(
                        "Can't delete seats from a conference that has been published at least once.");
                }

                context.Seats.Remove(seat);

                context.SaveChanges();
            }
        }

        private void PublishConferenceEvent<T>(ConferenceInfo conference) where T : ConferenceEvent, new() {
            this._eventBus.Publish(new T() {
                SourceId = conference.Id,
                Owner = new Owner() {
                    Name = conference.OwnerName,
                    Email = conference.OwnerEmail
                },
                Name = conference.Name,
                Description = conference.Description,
                Location = conference.Location,
                Slug = conference.Slug,
                Tagline = conference.Tagline,
                TwitterSearch = conference.TwitterSearch,
                StartDate = conference.StartDate,
                EndDate = conference.EndDate
            });
        }

        private void PublishSeatCreated(Guid conferenceId, SeatType seat) {
            this._eventBus.Publish(new SeatCreated() {
                ConferenceId = conferenceId,
                SourceId = seat.Id,
                Name = seat.Name,
                Description = seat.Description,
                Price = seat.Price,
                Quantity = seat.Quantity
            });
        }
    }
}