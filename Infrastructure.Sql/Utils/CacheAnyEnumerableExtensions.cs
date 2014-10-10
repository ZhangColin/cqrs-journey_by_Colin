using System.Collections;
using System.Collections.Generic;

namespace Infrastructure.Sql.Utils {
    public static class CacheAnyEnumerableExtensions {
        public static IAnyEnumerable<T> AsCachedAnyEnumerable<T>(this IEnumerable<T> source) {
            return new AnyEnumerable<T>(source);
        } 

        public interface IAnyEnumerable<out T> : IEnumerable<T> {
            bool Any();
        }

        private class AnyEnumerable<T>: IAnyEnumerable<T> {
            private readonly IEnumerable<T> _enumerable;
            private IEnumerator<T> _enumerator;
            private bool _hasAny;

            public AnyEnumerable(IEnumerable<T> enumerable) {
                this._enumerable = enumerable;
            }

            public bool Any() {
                this.InitializeEnumerator();
                return this._hasAny;
            }

            public IEnumerator<T> GetEnumerator() {
                this.InitializeEnumerator();
                return this._enumerator;
            }

            private void InitializeEnumerator() {
                if(this._enumerator==null) {
                    var inner = this._enumerable.GetEnumerator();
                    this._hasAny = inner.MoveNext();
                    this._enumerator = new SkipFirstEnumerator(inner, this._hasAny);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return this.GetEnumerator();
            }

            private class SkipFirstEnumerator: IEnumerator<T> {
                private readonly IEnumerator<T> _inner;
                private readonly bool _hasNext;
                private bool _isFirst = true;

                public SkipFirstEnumerator(IEnumerator<T> inner, bool hasNext) {
                    this._inner = inner;
                    this._hasNext = hasNext;
                }

                public void Dispose() {
                    this._inner.Dispose();
                }

                public bool MoveNext() {
                    if(this._isFirst) {
                        this._isFirst = false;
                        return this._hasNext;
                    }

                    return this._inner.MoveNext();
                }

                public void Reset() {
                    this._inner.Reset();
                }

                public T Current { get { return this._inner.Current; } }

                object IEnumerator.Current {
                    get { return Current; }
                }
            }
        }
    }
}