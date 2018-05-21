using System;
using System.Collections.Generic;

public interface IEmailService
{
	void Send(EmailMessage emailMessage, String attachmentFilePath);
	List<EmailMessage> ReceiveEmail(int maxCount = 10);
}
