using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

namespace Registration.ReadModel.Implementation {
    public class CachingConferenceDao: IConferenceDao {
        private readonly IConferenceDao _decoratedDao;
        private readonly ObjectCache _cache;

        public CachingConferenceDao(IConferenceDao decoratedDao, ObjectCache cache) {
            this._decoratedDao = decoratedDao;
            this._cache = cache;
        }

        public ConferenceDetails GetConferenceDetails(string conferenceCode) {
            var key = "ConferenceDao_Details_" + conferenceCode;
            var conference = this._cache.Get(key) as ConferenceDetails;
            if(conference==null) {
                conference = this._decoratedDao.GetConferenceDetails(conferenceCode);
                if(conference!=null) {
                    this._cache.Set(key, conference, new CacheItemPolicy{AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(10)});
                }
            }
            return conference;
        }

        public ConferenceAlias GetConferenceAlias(string conferenceCode) {
            var key = "ConferenceDao_Alias_" + conferenceCode;
            var conference = this._cache.Get(key) as ConferenceAlias;
            if (conference == null) {
                conference = this._decoratedDao.GetConferenceAlias(conferenceCode);
                if (conference != null) {
                    this._cache.Set(key, conference, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(10) });
                }
            }
            return conference;
        }

        public IList<ConferenceAlias> GetPublishedConferences() {
            var key = "ConferenceDao_PublishedConferences";
            var cached = this._cache.Get(key) as IList<ConferenceAlias>;
            if (cached == null) {
                cached = this._decoratedDao.GetPublishedConferences();
                if (cached != null) {
                    this._cache.Set(key, cached, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(10) });
                }
            }
            return cached;
        }

        public IList<SeatType> GetPublishedSeatTypes(Guid conferenceId) {
            var key = "ConferenceDao_PublishedSeatTypes_"+conferenceId;
            var seatTypes = this._cache.Get(key) as IList<SeatType>;
            if (seatTypes == null) {
                seatTypes = this._decoratedDao.GetPublishedSeatTypes(conferenceId);
                if (seatTypes != null) {
                    TimeSpan timeToCache;

                    if(seatTypes.All(x=>x.AvailableQuantity>200 || x.AvailableQuantity<=0)) {
                        timeToCache = TimeSpan.Zero;
                    }
                    else if(seatTypes.All(x=>x.AvailableQuantity<30 && x.AvailableQuantity>0)) {
                        timeToCache = TimeSpan.FromMinutes(5);
                    }
                    else if(seatTypes.All(x=>x.AvailableQuantity<100 && x.AvailableQuantity>0)) {
                        timeToCache = TimeSpan.FromSeconds(20);
                    }
                    else {
                        timeToCache = TimeSpan.FromMinutes(1);
                    }

                    if(timeToCache>TimeSpan.Zero) {
                        this._cache.Set(key, seatTypes, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(10) });
                    }
                }
            }
            return seatTypes;
        }

        public IList<SeatTypeName> GetSeatTypeNames(IEnumerable<Guid> seatTypes) {
            return this._decoratedDao.GetSeatTypeNames(seatTypes);
        }
    }
}