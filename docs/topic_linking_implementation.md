# AI Topic Generation & Linking Implementation

This document explains how the SmartInsights system handles **Topic Generation** and **Topic Linking** for student feedback, ensuring that related inputs are grouped together while new trends are identified automatically.

## 1. Overview

The system uses a **Hybrid AI Approach** to manage topics:
1.  **Context-Aware Generation:** The AI is aware of *existing* topics when analyzing new feedback.
2.  **Smart Linking:** It prioritizes linking feedback to an existing topic if it fits.
3.  **Dynamic Creation:** It generates a new topic only if the feedback represents a distinct, new issue.

## 2. The Workflow

### Step 1: Input Analysis
When a new student input (General Feedback) is received, it is first analyzed for **Sentiment**, **Tone**, **Urgency**, and **Theme** (e.g., Facilities, Academics).

### Step 2: Fetching Context
Before asking the AI to assign a topic, the system retrieves the list of **currently active topics** from the database.
*   *Optimization:* This list is filtered by the user's Department (if applicable) to ensure relevance.

### Step 3: AI Prompt Engineering
We construct a dynamic prompt for the AI (Gemini Flash Lite) that includes:
1.  The student's feedback body.
2.  The list of **Existing Topics**.
3.  Strict instructions to **match** an existing topic if possible.

**The Prompt Structure:**
```text
Based on this student feedback, assign it to an EXISTING topic from the list below, or generate a NEW concise topic name (3-6 words) if none fit.

Existing Topics:
- Engineering Building Wi-Fi Issues
- Parking Availability and Access
- Library Quiet Study Area Noise
...

Feedback: "[Student's message]"

Instructions:
1. If the feedback clearly belongs to an existing topic, return that EXACT topic name.
2. If it represents a new issue, generate a new concise name.
3. Return ONLY the topic name, nothing else.
```

### Step 4: Processing the AI Response
The AI returns a single string (the topic name).
1.  **Exact Match Check:** The system compares the returned name against the database (case-insensitive).
2.  **Link:** If a match is found, the input is linked to that `TopicId`.
3.  **Create:** If no match is found, a new `Topic` entity is created in the database, and the input is linked to it.

## 3. Role of Initial Seeding

To prevent the AI from generating fragmented topics at the start (e.g., "Bad Wi-Fi", "Wi-Fi Issues", "Internet Down"), we rely on **Initial Seeding** and **System Prompts**.

*   **Themes:** We seed high-level themes (Academics, Facilities, etc.) to guide the initial categorization.
*   **University Context:** The system prompt includes context about the university (KFUEIT), helping the AI understand specific terms like "Eduroam" or "Engineering Block".

## 4. Technical Components

*   **`GeminiAIService.GenerateOrFindTopicAsync`**: The core logic that builds the prompt and handles the API call.
*   **`AIProcessingJobs`**: The background job that orchestrates the process, ensuring it runs asynchronously without blocking the user UI.
*   **`AICostTrackingService`**: Tracks token usage to monitor the cost of this feature.

## 5. Benefits

*   **Reduced Noise:** Prevents duplicate topics for the same issue.
*   **Trend Detection:** Automatically surfaces new issues as they emerge.
*   **Historical Context:** Keeps long-running issues (like "Parking") grouped together over time, allowing for better trend analysis.
