// This file contains the AI-enhanced version of InputService.CreateAsync method
// Replace the CreateAsync method in InputService.cs with this implementation

/*
public async Task<InputDto> CreateAsync(CreateInputRequest request)
{
    // Validate user exists
    User? user = null;
    if (request.UserId.HasValue)
    {
        user = await _userRepository.GetByIdAsync(request.UserId.Value);
        if (user == null)
            throw new ArgumentException("User not found");
    }

    // Determine input type
    var inputType = request.InquiryId.HasValue ? InputType.InquiryLinked : InputType.General;

    // If inquiry-linked, validate inquiry exists and is active
    if (inputType == InputType.InquiryLinked && request.InquiryId.HasValue)
    {
        var inquiry = await _inquiryRepository.GetByIdAsync(request.InquiryId.Value);
        if (inquiry == null)
            throw new ArgumentException("Inquiry not found");
        
        if (inquiry.Status != InquiryStatus.Active)
            throw new InvalidOperationException("Inquiry is not active");
    }

    var input = new Input
    {
        Id = Guid.NewGuid(),
        Body = request.Body,
        Type = inputType,
        Status = InputStatus.Pending,
        UserId = request.UserId ?? Guid.Empty,
        InquiryId = request.InquiryId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    await _inputRepository.AddAsync(input);

    // ENHANCED: Trigger immediate AI processing
    try
    {
        input.Status = InputStatus.Processing;
        await _inputRepository.UpdateAsync(input);

        // Get AI analysis
        var analysis = await _aiService.AnalyzeInputAsync(input.Body, input.Type);
        
        // Update input with AI results
        input.Sentiment = analysis.Sentiment;
        input.Tone = analysis.Tone;
        input.UrgencyPct = analysis.Urgency;
        input.ImportancePct = analysis.Importance;
        input.ClarityPct = analysis.Clarity;
        input.QualityPct = analysis.Quality;
        input.HelpfulnessPct = analysis.Helpfulness;
        input.CalculateScore(); // This sets Score and Severity
        input.Status = InputStatus.Processed;
        
        // For general inputs, extract theme and generate/find topic
        if (input.Type == InputType.General)
        {
            // Find theme
            var themes = await _themeRepository.GetAllAsync();
            var matchingTheme = themes.FirstOrDefault(t => 
                t.Name.Equals(analysis.ExtractedTheme, StringComparison.OrdinalIgnoreCase));
            
            if (matchingTheme != null)
            {
                input.ThemeId = matchingTheme.Id;
            }

            // Generate or find topic
            var departmentId = user?.DepartmentId;
            var topic = await _aiService.GenerateOrFindTopicAsync(input.Body, departmentId);
            input.TopicId = topic.Id;
        }
        
        input.UpdatedAt = DateTime.UtcNow;
        await _inputRepository.UpdateAsync(input);
    }
    catch (Exception ex)
    {
        // Log error but don't fail the operation
        input.Status = InputStatus.Error;
        await _inputRepository.UpdateAsync(input);
    }

    return await GetByIdAsync(input.Id) 
        ?? throw new InvalidOperationException("Failed to retrieve created input");
}
*/
