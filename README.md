# Gambling5.de Website

Official band website for **Gambling 5**.

## Overview

- **Domain**: gambling5.de
- **Type**: Band Website
- **Hosting**: Azure Static Web Apps
- **DNS**: Azure DNS (already configured)
- **Framework**: React / Next.js (To be decided)

## Website Features

### Core Pages
1. **Home** - Band introduction and hero section
2. **About** - Band story, members, music style
3. **Gigs** - Upcoming shows and past performances
4. **Contact** - Contact form (sends to info@gambling5.de)

### Additional Features
- Photo gallery
- Music samples/links
- Social media integration
- Responsive mobile-first design

## Project Structure

```
gambling5-website/
├── src/
│   ├── components/         # React components
│   ├── pages/             # Page components
│   └── styles/            # CSS/styling
├── public/
│   └── images/            # Website images (logo, backgrounds)
├── infrastructure/        # Azure deployment scripts
├── docs/
│   └── Images/           # Design inspiration (NOT for website use)
└── README.md
```

## Design Assets

- **Logo**: `Gambling5.Web/wwwroot/images/logo.png`
- **Background**: `Gambling5.Web/wwwroot/images/background.png`
- **Inspiration**: See `docs/Images/` for pamphlets and business cards

## Technology Stack

**Frontend:**
- React or Next.js
- Modern CSS / Tailwind CSS
- Responsive design

**Backend/Contact:**
- Azure Functions for contact form
- Email integration (info@gambling5.de)

**Hosting:**
- Azure Static Web Apps (Free tier)
- Automatic HTTPS
- Global CDN
- CI/CD with GitHub

## Cost Estimate

- **Hosting**: Free (Azure Static Web Apps free tier)
- **Domain**: Already registered
- **DNS**: ~$0.50/month
- **Total**: ~$0.50/month

## Getting Started

### Prerequisites
- .NET 9 SDK (installed ✓)
- Visual Studio 2022 or VS Code with C# extension

### Run Locally
```bash
cd Gambling5.Web
dotnet watch run
```
Visit: https://localhost:5001

### Build for Production
```bash
dotnet publish -c Release
```

### Next Steps
1. Customize pages (Home, About, Gigs, Contact)
2. Style based on band identity
3. Test contact form
4. Deploy to Azure Static Web Apps
5. Connect gambling5.de domain

---

**Status**: Setup Phase
**Band**: Gambling 5
**Last Updated**: 2026-01-24
