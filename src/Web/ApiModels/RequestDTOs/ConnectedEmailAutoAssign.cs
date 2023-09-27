﻿namespace Web.ApiModels.RequestDTOs
{
    public class ConnectedEmailAutoAssign
    {
        public string email { get; set; }
        public bool autoAssign { get; set; }
    }

    public class ToggleCalendarDTO
    {
        public bool toggle { get; set; }
        public string email { get; set; }
    }
}
