using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Xml;

namespace Message_Handler
{
    public class MessageHandler
    ///<summary>
    /// This handles any messages being passed by the application including warnings, alerts and fatal errors.
    /// 
    /// Key for logging levels:
    /// 1 - Fatal   Data loss is likely to have occurred, or is likely to occur, as a result of whatever just happened
    /// 2 - Error   Application cannot function correctly following this event, and will terminate
    /// 3 - Warn    Application was stopped from doing something, but can keep running (maybe switched to a backup, or required information is missing)
    /// 4 - Info    Useful information about what just happened, maybe a service started or a connection was established
    /// 5 - Debug   Information useful for technicians or sysadmins to troubleshoot an issue
    /// 6 - Trace   Application has an itch on its nose that the developer might want to know about
    ///</summary>
    {
        private static bool _debugMode = false; // Enabling Debug Mode causes message boxes to appear for every little thing, including Trace
        public static bool debugMode
        {
            set { _debugMode = value; }
            get { return _debugMode; }
        }

        private static int _eventViewerLoggingLevel = 0; // Defines what level of error will be logged in Event Viewer; defaults to 2 or lower, set to 0 currently to disable function
        public static int eventViewerLoggingLevel
        {
            get { return _eventViewerLoggingLevel; }
            set { _eventViewerLoggingLevel = value; }
        }

        private static int _applicationLogFileLoggingLevel = 4; // Defines what level of error will be logged in the Application log; defaults to 4 or lower
        public static int applicationLogFileLoggingLevel
        {
            get { return _applicationLogFileLoggingLevel; }
            set { _applicationLogFileLoggingLevel = value; }
        }

        private static string _applicationLogFileLocation = null; // Contains full file path to the application logs
        public static string applicationLogFileLocation
        {
            get { return _applicationLogFileLocation; }
            set { _applicationLogFileLocation = value; }
        }

        /// <summary>
        /// Handles logging of messages for events that the application throws out
        /// </summary>
        /// <param name="showMessagebox">"True" will force a message box to appear regardless of application settings</param>
        /// <param name="messageLevel">Integer determining error level based on above legend</param>
        /// <param name="error">Exception returned from try-catch statement; may be null</param>
        /// <param name="context">What the application was doing at the time of the message</param>
        public static void handleMessage(bool showMessagebox, int messageLevel, Exception error, string context)
        {
            MessageLevelDefinition messageDef = MessageLevel.level(messageLevel);

            if (messageLevel <= _eventViewerLoggingLevel)
            {
                logEventViewerEvent(messageDef, error, context);
            }

            if (messageLevel <= _applicationLogFileLoggingLevel)
            {
                logApplicationLogFileEntry(messageDef, error, context);
            }

            if (showMessagebox || _debugMode)
            {
                if (error != null)
                {
                    MessageBox.Show(messageDef.messageBoxIntro + "\n\nContext:\n" + context + "\n\nError message:\n" + error.Message, messageDef.messageBoxTitle);
                }
                else
                {
                    MessageBox.Show(messageDef.messageBoxIntro + "\n\nContext:\n" + context + "\n\nNo Exception object generated", messageDef.messageBoxTitle);
                }
            }
        }

        /// <summary>
        /// Currently not functional, need an installer to create the Event Viewer source
        /// Logs an event in the Windows Event Viewer, spawns a message box if this fails
        /// </summary>
        /// <param name="messageDef">Definition of the message type</param>
        /// <param name="error">Exception, if there was one</param>
        /// <param name="context">Specifies what the application was doing when the error occurred</param>
        private static void logEventViewerEvent(MessageLevelDefinition messageDef, Exception error, string context)
        {
            string eventContent = "Logged at " + DateTime.Now + " (" + DateTime.UtcNow + ") UTC\n\n"
                + "Error Message: " + error.Message + "\n\n"
                + "Application error level: " + messageDef.levelName + "\n\n"
                + "HResult: " + error.HResult + "\n\n"
                + "Source: " + error.Source + "\n\n"
                + "TargetSite: " + error.TargetSite + "\n\n"
                + "Stack Trace:\n" + error.StackTrace;

            try
            {
                if (!EventLog.SourceExists(error.Source))
                {
                    EventLog.CreateEventSource(error.Source, "Application");
                }

                EventLog.WriteEntry(error.Source, eventContent, messageDef.windowsErrorLevel);
            }
            catch(Exception e)
            {
                MessageBox.Show("Unable to log event viewer message: " + e.Message + "\n\n" + eventContent);
            }
        }

        /// <summary>
        /// Saves events to the application log file, creating the file if it doesn't exist
        /// </summary>
        /// <param name="messageDef">Definition of the message type</param>
        /// <param name="error">Exception, if there was one</param>
        /// <param name="context">Specifies what the application was doing when the error occurred</param>
        private static void logApplicationLogFileEntry(MessageLevelDefinition messageDef, Exception error, string context)
        {
            // Confirm the log file location has been specified
            if (_applicationLogFileLocation == null)
            {
                return;
            }

            // Create the log file, if it doesn't exist
            if (!File.Exists(_applicationLogFileLocation))
            {
                try
                {
                    FileInfo logFileInfo = new FileInfo(_applicationLogFileLocation);
                    Directory.CreateDirectory(logFileInfo.Directory.FullName);

                    XmlDocument newDoc = new XmlDocument();
                    newDoc.LoadXml("<logs></logs>");

                    XmlTextWriter xmlWriter = new XmlTextWriter(_applicationLogFileLocation, null);
                    xmlWriter.Formatting = Formatting.Indented;
                    newDoc.Save(xmlWriter);

                    xmlWriter.Dispose();
                }
                catch(Exception e)
                {
                    MessageBox.Show("Unable to create log file in " + _applicationLogFileLocation + "\n\n"
                        + e.Message);
                }
            }

            // Load the Xml document
            XmlDocument logFile = new XmlDocument();
            logFile.Load(_applicationLogFileLocation);

            XmlNode rootNode = logFile.SelectSingleNode("logs");

            // Build the log entry
            XmlNode newNode = logFile.CreateElement("log");

            XmlAttribute utcTimeAttribute = logFile.CreateAttribute("utctime");
            utcTimeAttribute.Value = DateTime.UtcNow.ToString();
            newNode.Attributes.Append(utcTimeAttribute);

            XmlAttribute levelAttribute = logFile.CreateAttribute("level");
            levelAttribute.Value = messageDef.levelName;
            newNode.Attributes.Append(levelAttribute);

            XmlAttribute contextAttribute = logFile.CreateAttribute("context");
            contextAttribute.Value = context;
            newNode.Attributes.Append(contextAttribute);

            if (error != null)
            {
                newNode.InnerText = error.Message;
            }
            else
            {
                newNode.InnerText = "No Exception object for event";
            }

            // Append the log entry
            rootNode.AppendChild(newNode);

            // Save the log file
            logFile.Save(_applicationLogFileLocation);
        }
    }

    /// <summary>
    /// MessageLevel contains code for defining each error level, along with message box text and Windows event viewer log levels
    /// </summary>
    public class MessageLevel
    {
        private static Dictionary<int, MessageLevelDefinition> _messageLevelDict = null;
        public Dictionary<int, MessageLevelDefinition> messageLevelDict
        {
            get { return _messageLevelDict; }
        }

        /// <summary>
        /// Adds default message definition to _messageLevelDict
        /// </summary>
        private static void buildDefaultDict()
        {
            if (_messageLevelDict == null)
            {
                _messageLevelDict = new Dictionary<int, MessageLevelDefinition>();
            }

            addMessageDefinition(1, "Fatal", "Fatal error", "A fatal error has occurred.", EventLogEntryType.Error);
            addMessageDefinition(2, "Error", "Application error", "An error has occurred in the application.", EventLogEntryType.Error);
            addMessageDefinition(3, "Warn", "Warning", "The application has generated a warning alert.", EventLogEntryType.Warning);
            addMessageDefinition(4, "Info", "Information", "Information has been generated", EventLogEntryType.Information);
            addMessageDefinition(5, "Debug", "Debug alert", "A debug notification was triggered", EventLogEntryType.Information);
            addMessageDefinition(6, "Trace", "Trace alert", "A trace notification was triggered", EventLogEntryType.Information);
        }

        /// <summary>
        /// Adds a new message definition to _messageLevelDict, or updates an existing one
        /// </summary>
        /// <param name="levelNumber">Integer code for the message level</param>
        /// <param name="levelName">Short name for the message level (e.g. "Warn", "Fatal", "Info")</param>
        /// <param name="messageBoxTitle">Title that will be used in any MessageBox created for this message</param>
        /// <param name="messageBoxIntro">Intro text that will be used in any MessageBox created for this message</param>
        /// <param name="windowsErrorLevel">Windows error level, used when creating an event in Event Viewer</param>
        public static void addMessageDefinition(int levelNumber, string levelName, string messageBoxTitle, string messageBoxIntro, EventLogEntryType windowsErrorLevel)
        {
            if (_messageLevelDict == null)
            {
                buildDefaultDict();
            }

            MessageLevelDefinition levelDef = new MessageLevelDefinition();
            levelDef.levelName = levelName;
            levelDef.messageBoxTitle = messageBoxTitle;
            levelDef.messageBoxIntro = messageBoxIntro;
            levelDef.windowsErrorLevel = windowsErrorLevel;

            if (_messageLevelDict.ContainsKey(levelNumber))
            {
                _messageLevelDict[levelNumber] = levelDef;
            }
            else
            {
                _messageLevelDict.Add(levelNumber, levelDef);
            }
        }

        /// <summary>
        /// When given an integer representation of a message level, returns the full MessageDefinition. Returns a default MessageDefinition if none is found.
        /// </summary>
        /// <param name="levelNumber">Integer representing message level - matches with the index in the _messageLevelDict</param>
        /// <returns></returns>
        public static MessageLevelDefinition level(int levelNumber)
        {
            if (_messageLevelDict == null)
            {
                buildDefaultDict();
            }

            if (_messageLevelDict.ContainsKey(levelNumber))
            {
                return _messageLevelDict[levelNumber];
            }
            else
            {
                MessageLevelDefinition unconfiguredErrorMessage = new MessageLevelDefinition();
                unconfiguredErrorMessage.levelName = "Incorrectly Handled Error";
                unconfiguredErrorMessage.messageBoxTitle = "Incorrectly Handled Error";
                unconfiguredErrorMessage.messageBoxIntro = "An incorrectly handled error has occurred. Please report this to the developer, who will need to ensure that the error level number has been added to MessageLevel._messageLevelDict.";
                unconfiguredErrorMessage.windowsErrorLevel = EventLogEntryType.Warning;

                return unconfiguredErrorMessage;
            }
        }
    }

    public class MessageLevelDefinition
    {
        public string levelName { get; set; }
        public string messageBoxTitle { get; set; }
        public string messageBoxIntro { get; set; }
        public EventLogEntryType windowsErrorLevel { get; set; }
    }
}