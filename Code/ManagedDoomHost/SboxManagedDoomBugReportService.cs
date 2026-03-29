using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sandbox;

namespace ManagedDoom
{
    public static class SboxManagedDoomBugReportService
    {
        private const string Endpoint = "https://bugs.akuji.org/api/bug-reports";

        private static readonly object Sync = new();
        private static readonly Queue<BugReportRequest> Pending = new();
        private static readonly Queue<string> Results = new();

        private static bool inFlight;

        public static void QueueSubmit(string contact, string map, string details, string currentState, string saveSlot)
        {
            lock (Sync)
            {
                Pending.Enqueue(new BugReportRequest(
                    contact?.Trim() ?? string.Empty,
                    map?.Trim() ?? string.Empty,
                    details?.Trim() ?? string.Empty,
                    currentState?.Trim() ?? string.Empty,
                    saveSlot?.Trim() ?? string.Empty));
            }
        }

        public static bool TryBeginSubmit(out BugReportRequest request)
        {
            lock (Sync)
            {
                if (inFlight || Pending.Count == 0)
                {
                    request = null;
                    return false;
                }

                inFlight = true;
                request = Pending.Dequeue();
                return true;
            }
        }

        public static bool TryConsumeResult(out string message)
        {
            lock (Sync)
            {
                if (Results.Count == 0)
                {
                    message = null;
                    return false;
                }

                message = Results.Dequeue();
                return true;
            }
        }

        public static async Task SubmitAsync(BugReportRequest request)
        {
            try
            {
                var payload = new
                {
                    contact = request.Contact,
                    map = request.Map,
                    details = request.Details,
                    currentState = request.CurrentState,
                    saveSlot = request.SaveSlot,
                    gameVersion = "sbox-doom-port",
                    build = "public"
                };

                await Http.RequestAsync(
                    Endpoint,
                    "POST",
                    Http.CreateJsonContent(payload));

                Complete("bug report submitted\nthank you");
            }
            catch (Exception ex)
            {
                Log.Warning($"[ManagedDoomHost] Bug report submission failed: {ex}");
                Complete("bug report failed\nplease try again later");
            }
        }

        private static void Complete(string message)
        {
            lock (Sync)
            {
                inFlight = false;
                Results.Enqueue(message);
            }
        }

        public sealed class BugReportRequest
        {
            public BugReportRequest(string contact, string map, string details, string currentState, string saveSlot)
            {
                Contact = contact;
                Map = map;
                Details = details;
                CurrentState = currentState;
                SaveSlot = saveSlot;
            }

            public string Contact { get; }
            public string Map { get; }
            public string Details { get; }
            public string CurrentState { get; }
            public string SaveSlot { get; }
        }
    }
}
