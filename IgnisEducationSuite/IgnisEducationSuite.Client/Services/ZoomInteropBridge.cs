using EDUSphereSharedProject.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace IgnisEducationSuite.Client.Services
{
    public class ZoomInteropBridge
    {

        public static ZoomInteropBridge? Instance { get; private set; }
        private readonly NavigationManager _nav;
        private readonly HttpClient _http;
        private readonly AppState _state;

        public LiveMeeting currentMeeting { get;  set; } = new LiveMeeting();

        public ZoomInteropBridge(NavigationManager nav, HttpClient http, AppState state)
        {
            _nav = nav;
            _http = http;
            _state = state;
            Instance = this;
        }

        public async Task HandleMeetingEndedAsync()
        {
            Console.WriteLine("Zoom meeting ended");

            // OPTIONAL: Mark meeting done
            // await _http.PostAsync("api/zoom/mark-done", null);
            // Navigate depending on role
            if (_state.UserRole == "Student")
            {
                _nav.NavigateTo("/StudentLessons");
            }
            else
            {
                _nav.NavigateTo("/createliveclass");
            }
        }
    }

}

