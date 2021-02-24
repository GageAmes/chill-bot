using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Reiati.ChillBot.Tools;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// Helper methods for <see cref="FileBasedGuildRepository"/>.
    /// </summary>
    public static class FileBasedGuildRepositoryExtensions
    {
        /// <summary>
        /// Keeps trying to checkout a guild (with backoff) until either maxTimeout has been hit,
        /// or a value has been returned that's not Locked.
        /// </summary>
        /// <param name="repo">A repo. May not be null.</param>
        /// <param name="guildId">An id associated with a guild.</param>
        /// <param name="maxTimeout">The maximum amount of time to spend waiting for this to unlock.</param>
        /// <param name="recycleResult">A preallocated result that should be returned if passed in.</param>
        /// <returns>The result.</returns>
        public static async Task<FileBasedGuildRepository.CheckoutResult> WaitForNotLockedCheckout(
            this FileBasedGuildRepository repo,
            Snowflake guildId,
            TimeSpan maxTimeout,
            FileBasedGuildRepository.CheckoutResult recycleResult = null)
        {
            Stopwatch timer = new Stopwatch();
            var retVal = recycleResult ?? new FileBasedGuildRepository.CheckoutResult();
            TimeSpan nextDelay = TimeSpan.FromMilliseconds(1);

            timer.Start();
            retVal = await repo.Checkout(guildId, retVal);

            while (retVal.Result == FileBasedGuildRepository.CheckoutResult.ResultType.Locked
                && timer.Elapsed + nextDelay < maxTimeout)
            {
                await Task.Delay(nextDelay);
                retVal = await repo.Checkout(guildId, retVal);
                nextDelay *= 1.5;
            }

            return retVal;
        }
    }
}
