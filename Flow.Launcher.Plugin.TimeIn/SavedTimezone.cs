namespace Flow.Launcher.Plugin.TimeIn
{
    public class SavedTimezoneItem:BaseModel
    {

        private string _ianaTimeZone;
        public string IanaTimeZone
        {
            get=>_ianaTimeZone;
            set
            {
                _ianaTimeZone = value;
                OnPropertyChanged();
            }
        }

        public SavedTimezoneItem(string ianaTimeZone)
        {
            IanaTimeZone = ianaTimeZone;
        }
    }
}