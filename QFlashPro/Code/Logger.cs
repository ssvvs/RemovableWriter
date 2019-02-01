namespace QFlashPro.Code
{
    using System;
    using System.IO;

    public class Logger
    {
        private bool _enabled;
        private string _filePath;

        public Logger()
        {
            _enabled = false;

        }

        public void SetSettings(string[] args)
        {
            try
            {
                if (args.Length != 1)
                    return;

                _filePath = args[0];

                using (FileStream sw = File.Create(_filePath))
                {
                }
                _enabled = true;
            }
            catch (Exception ex)
            {
                _enabled = false;
            }
        }

        public void Debug(Func<string> action)
        {
            if (!_enabled)
                return;
            try
            {
                string s = action.Invoke();
                File.AppendAllText(_filePath, $"{DateTime.UtcNow} - {s} \n");
            }
            catch (Exception ex)
            {

            }
        }
    }
}
