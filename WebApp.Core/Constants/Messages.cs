namespace WebApp.Core.Constants
{
    public static class Messages
    {
        public static string GeneralError => "Something went wrong. Please try again later.";
        public static string JsonParseError => "Message could not be read";
        public static string RabbitMqClosed => "RabbitMQ closed";
        public static string EnqueueSucceeded => "Enqueue Succeeded";
        public static string RabbitMqConnectionError => "Rabbit MQ connection failed";
        public static string QueueListening => "queue listening..";
        public static string EmailSent => "E-mail was sent successfully";

    }
}