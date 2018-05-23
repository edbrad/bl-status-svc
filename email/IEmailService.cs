using System;
using System.Collections.Generic;

/// <summary>
/// Describe an Email Service
/// </summary>
public interface IEmailService
{
	void Send(EmailMessage emailMessage, String attachmentFilePath);
	List<EmailMessage> ReceiveEmail(int maxCount = 10);
}
