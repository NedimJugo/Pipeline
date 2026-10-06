using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pipeline.Domain.Entities;
using Pipeline.Domain.Enums;
using JobApplication = Pipeline.Domain.Entities.Application;

namespace Pipeline.Infrastructure.Persistence;

public static class NedimDataSeeder
{
    public const string NedimEmail = "nedim.jugoo@gmail.com";

    public static async Task<User> SeedAsync(
        PipelineDbContext db,
        UserManager<User> userManager,
        Guid? targetUserId = null,
        CancellationToken ct = default)
    {
        User? user = null;

        if (targetUserId.HasValue)
        {
            user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == targetUserId.Value, ct);
        }

        if (user == null)
        {
            user = await userManager.FindByEmailAsync(NedimEmail);
        }

        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                UserName = NedimEmail,
                Email = NedimEmail,
                DisplayName = "Nedim Jugo",
                PhoneNumber = "+387 60 318 5869",
                Location = "Mostar, BiH",
                TargetRole = "Junior Software Developer / Junior Project Manager",
                Seniority = "Junior",
                SalaryExpectationMin = 1750,
                SalaryExpectationMax = 2000,
                Currency = "BAM",
                SearchStatus = SearchStatus.Active,
                Timezone = "Europe/Sarajevo",
                OnboardingCompleted = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(user, "Password123!");
            if (!createResult.Succeeded)
            {
                db.Users.Add(user);
                await db.SaveChangesAsync(ct);
            }
        }
        else
        {
            user.DisplayName = "Nedim Jugo";
            user.PhoneNumber = "+387 60 318 5869";
            user.Location = "Mostar, BiH";
            user.TargetRole = "Junior Software Developer / Junior Project Manager";
            user.Seniority = "Junior";
            user.SalaryExpectationMin = 1750;
            user.SalaryExpectationMax = 2000;
            user.Currency = "BAM";
            user.SearchStatus = SearchStatus.Active;
            user.OnboardingCompleted = true;
            user.UpdatedAt = DateTime.UtcNow;

            if (!await userManager.CheckPasswordAsync(user, "Password123!"))
            {
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                await userManager.ResetPasswordAsync(user, resetToken, "Password123!");
            }

            await db.SaveChangesAsync(ct);
        }

        var userId = user.Id;

        // Clean slate for Nedim to ensure exact real data without old mock leftovers
        var existingAppContacts = await db.ApplicationContacts.IgnoreQueryFilters().Where(ac => ac.UserId == userId).ToListAsync(ct);
        db.ApplicationContacts.RemoveRange(existingAppContacts);

        var existingInteractions = await db.Interactions.IgnoreQueryFilters().Where(i => i.UserId == userId).ToListAsync(ct);
        db.Interactions.RemoveRange(existingInteractions);

        var existingHistories = await db.ApplicationStatusHistories.IgnoreQueryFilters().Where(h => h.UserId == userId).ToListAsync(ct);
        db.ApplicationStatusHistories.RemoveRange(existingHistories);

        var existingInterviews = await db.Interviews.IgnoreQueryFilters().Where(i => i.UserId == userId).ToListAsync(ct);
        db.Interviews.RemoveRange(existingInterviews);

        var existingTasks = await db.Tasks.IgnoreQueryFilters().Where(t => t.UserId == userId).ToListAsync(ct);
        db.Tasks.RemoveRange(existingTasks);

        var existingApps = await db.Applications.IgnoreQueryFilters().Where(a => a.UserId == userId).ToListAsync(ct);
        db.Applications.RemoveRange(existingApps);

        var existingContacts = await db.Contacts.IgnoreQueryFilters().Where(c => c.UserId == userId).ToListAsync(ct);
        db.Contacts.RemoveRange(existingContacts);

        await db.SaveChangesAsync(ct);

        // Helper to find or create company
        async Task<Company> GetOrCreateCompany(string name, string? website = null, string? location = null, string? industry = null)
        {
            var comp = await db.Companies.FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower(), ct);
            if (comp == null)
            {
                comp = new Company
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Website = website,
                    Location = location,
                    Industry = industry,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.Companies.Add(comp);
                await db.SaveChangesAsync(ct);
            }
            return comp;
        }

        // Helper to create contact
        Contact CreateContact(
            string fullName,
            Guid? companyId,
            string? role = null,
            string? email = null,
            string? phone = null,
            string? linkedIn = null,
            ContactType type = ContactType.Recruiter,
            string? notes = null)
        {
            var contact = new Contact
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CompanyId = companyId,
                FullName = fullName,
                Role = role,
                Email = email,
                Phone = phone,
                LinkedInUrl = linkedIn,
                Type = type,
                Notes = notes,
                LastContactedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Contacts.Add(contact);
            return contact;
        }

        // Helper to create application
        JobApplication CreateApplication(
            Company company,
            string roleTitle,
            ApplicationStatus status,
            DateTime appliedAt,
            ApplicationSource source,
            string? sourceDetail,
            WorkMode workMode,
            string? notes,
            string? closedReason,
            decimal? offerSalary = null,
            string? offerNotes = null)
        {
            var app = new JobApplication
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CompanyId = company.Id,
                RoleTitle = roleTitle,
                Status = status,
                StatusChangedAt = appliedAt.AddDays(status == ApplicationStatus.Applied ? 0 : 5),
                AppliedAt = appliedAt,
                Source = source,
                SourceDetail = sourceDetail,
                WorkMode = workMode,
                EmploymentType = EmploymentType.FullTime,
                Location = company.Location ?? "Mostar, BiH",
                Notes = notes,
                ClosedReason = closedReason,
                OfferSalary = offerSalary,
                Currency = "BAM",
                OfferNegotiationNotes = offerNotes,
                CreatedAt = appliedAt,
                UpdatedAt = DateTime.UtcNow
            };
            db.Applications.Add(app);

            // Add status history
            db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ApplicationId = app.Id,
                FromStatus = ApplicationStatus.Wishlist,
                ToStatus = ApplicationStatus.Applied,
                ChangedAt = appliedAt,
                Note = "Prijava evidentirana u sistemu."
            });

            if (status != ApplicationStatus.Applied)
            {
                db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ApplicationId = app.Id,
                    FromStatus = ApplicationStatus.Applied,
                    ToStatus = status,
                    ChangedAt = app.StatusChangedAt,
                    Note = closedReason ?? $"Ažuriran status na {status}."
                });
            }

            return app;
        }

        // Helper to add interaction
        void AddInteraction(
            JobApplication? app,
            Contact? contact,
            InteractionDirection direction,
            InteractionChannel channel,
            DateTime occurredAt,
            string summary,
            string sentContent)
        {
            var inter = new Interaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ApplicationId = app?.Id,
                ContactId = contact?.Id,
                Direction = direction,
                Channel = channel,
                OccurredAt = occurredAt,
                Summary = summary,
                SentContent = sentContent,
                CreatedAt = occurredAt,
                UpdatedAt = occurredAt
            };
            db.Interactions.Add(inter);
        }

        // Helper to link application to contact
        void LinkApplicationContact(JobApplication app, Contact contact)
        {
            db.ApplicationContacts.Add(new ApplicationContact
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ApplicationId = app.Id,
                ContactId = contact.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // ==========================================
        // 1. RAIFFEISEN GROUP (Ghosted)
        // ==========================================
        var compRaiffeisen = await GetOrCreateCompany("Raiffeisen Group", "https://bosnia.recruiter.hr", "Sarajevo / BiH", "Banking & Finance");
        var contRaiffeisen = CreateContact("Raiffeisen Recruiter", compRaiffeisen.Id, "Recruitment Team", "raiffeisengroup@ba.recruiter.hr", null, null, ContactType.Recruiter, "Pristupni nalog bosnia.recruiter.hr");
        var appRaiffeisen = CreateApplication(
            compRaiffeisen,
            "Junior Software Developer / IT pozicija",
            ApplicationStatus.Ghosted,
            new DateTime(2026, 9, 1, 13, 49, 0, DateTimeKind.Utc),
            ApplicationSource.CompanyWebsite,
            "https://bosnia.recruiter.hr",
            WorkMode.Hybrid,
            "Aplikacija preko stranice https://bosnia.recruiter.hr. Dobio pristupne podatke i potvrdu prijave. Drugi odgovor nikad nisam dobio.",
            "Drugi odgovor nikad nisam dobio.");
        LinkApplicationContact(appRaiffeisen, contRaiffeisen);

        AddInteraction(appRaiffeisen, contRaiffeisen, InteractionDirection.Inbound, InteractionChannel.Email,
            new DateTime(2026, 9, 1, 13, 49, 0, DateTimeKind.Utc),
            "Obavijest o prijavi na konkurs - Raiffeisen Group",
@"Recruiter
Poštovani/a,
Obavijest o prijavi na konkurs

Poslodavac Raiffeisen Group Vam se zahvaljuje na prijavi te Vas obavještava da na adresi https://bosnia.recruiter.hr možete dodatno urediti svoj profil ili ostaviti informacije koje mogu biti korisne za nastavak selekcijskog postupka, a to možete učiniti sa sljedećim pristupnim podacima:

Korisničko ime : ri9tg1h3
Lozinka : WrMpqwtE

Poslodavac Raiffeisen Group Vam se zahvaljuje što ste odvojili vrijeme i prijavili se na konkurs. U svom korisničkom profilu možete izmijeniti lozinku, dodati svoju sliku te ostaviti ili povući pristanak za obradu ličnih podataka.

Raiffeisen Group i Recruiter

*Molimo Vas da nakon što se prvi put prijavite u sistem, radi dodatne sigurnosti vaših ličnih podataka, promijenite lozinku koju ste dobili putem e-maila. Prijavom u sistem potvrđujete da ste upoznati i da se slažete s našom politikom privatnosti i uvjetima korištenja. Također Vas obavještavamo da brinemo o Vašoj privatnosti te ćemo obrađivati Vaše lične podatke samo u svrhu obrade prijave na konkurs za posao, sukladno članku 6. stavku 1(f) Opšte regulative o zaštiti podataka (""GDPR""). Društva koja će obrađivati Vaše podatke, kao voditelj obrade, je Raiffeisen Group, a kao izvršitelj obrade, je Recruiter. Obrada Vaših ličnih podataka je neophodna za postupak prijave na konkurs za posao. U slučaju da ne želite dati lične podatke, nećemo biti u mogućnosti obraditi Vašu prijavu za konkurs na posao. Vaši podaci će biti čuvani toliko dugo koliko je potrebno da obradimo Vašu prijavu za posao i provedemo selekcijski postupak, uz iznimku za osobe koje su zaposlene tokom konkursa. Lične podatke ćemo obrađivati i nakon konkursa za posao isključivo ako ste dali pristanak za takvu obradu. Naši serveri nalaze se u Francuskoj pa Vaši podaci u enkriptiranom digitalnom obliku mogu biti preneseni i spremljeni i u toj zemlji. Vaša prava za zaštitu ličnih podataka, sukladno uvjetima propisanim GDPR-om, su: pravo na pristup ličnim podacima te pravo na ispravak, brisanje ili ograničenje obrade Vaših ličnih podataka; pravo na prigovor na obradu te pravo na prenosivost Vaših ličnih podataka koji se obrađuju. U slučaju da želite ostvariti neko od navedenih prava, možete nam se obratiti na e-mail adresu gdpr@ba.recruiter.hr. Također, imate pravo podnijeti pritužbu u vezi obrade Vaših ličnih podataka Agenciji za zaštitu ličnih podataka.");

        // ==========================================
        // 2. UNICREDIT BANK D.D. (Withdrawn / Declined Offer)
        // ==========================================
        var compUniCredit = await GetOrCreateCompany("UniCredit Bank d.d.", "https://www.unicredit.ba", "Mostar, BiH", "Banking");
        var contUniCredit = CreateContact("UniCredit Prijava za posao", compUniCredit.Id, "HR / Recruitment", "prijavazaposao@unicreditgroup.ba", null, null, ContactType.Recruiter);
        var appUniCredit = CreateApplication(
            compUniCredit,
            "Junior Software Developer / IT sektor",
            ApplicationStatus.Withdrawn,
            new DateTime(2026, 8, 25, 10, 0, 0, DateTimeKind.Utc),
            ApplicationSource.Other,
            "prijavazaposao@unicreditgroup.ba",
            WorkMode.Onsite,
            "Zvali me preko telefona ponudili u call centru 1450KM da radim ja odbio",
            "Ponudili poziciju u call centru umjesto IT sektora sa platom 1450 KM, ponuda odbijena.",
            1450,
            "Zvali me preko telefona ponudili u call centru 1450KM da radim ja odbio");
        LinkApplicationContact(appUniCredit, contUniCredit);

        AddInteraction(appUniCredit, contUniCredit, InteractionDirection.Outbound, InteractionChannel.Email,
            new DateTime(2026, 8, 25, 10, 0, 0, DateTimeKind.Utc),
            "Iskaz interesa za rad u IT sektoru UniCredit banke – Nedim Jugo",
@"Poštovani UniCredit timu,

Javljam Vam se kako bih iskazao interes za rad u IT sektoru UniCredit banke. Kao diplomant softverskog inženjeringa, izuzetno cijenim Vaš pristup tehnološkom razvoju i inovacijama u bankarstvu.

Motivisan sam da svoju karijeru gradim u okruženju koje pruža priliku za kontinuirano učenje od vrhunskih stručnjaka, ali u kojem istovremeno mogu aktivno doprinijeti radu i uspjehu tima svojim dosadašnjim znanjem, radnim navikama i tehničkim vještinama.

U prilogu Vam dostavljam svoj CV i motivaciono pismo, sa željom da pregledate moje kvalifikacije i projekte. Nadam se da ćete razmotriti moju prijavu za trenutne ili nadolazeće prilike u kojima bi moj profil mogao biti od koristi.

Bilo bi mi zadovoljstvo da Vam se i lično predstavim na razgovoru.

Unaprijed se zahvaljujem na izdvojenom vremenu i pažnji.

Ugodan dan,

Nedim Jugo
+38760 318 5869
LinkedIn GitHub");

        AddInteraction(appUniCredit, contUniCredit, InteractionDirection.Inbound, InteractionChannel.Phone,
            new DateTime(2026, 9, 2, 11, 30, 0, DateTimeKind.Utc),
            "Telefonski poziv – Ponuda za rad u call centru (1450 KM)",
            "Zvali me preko telefona ponudili u call centru 1450KM da radim ja odbio");

        // ==========================================
        // 3. EDIN SALIHAGIĆ (Networking & Preporuke - Applied)
        // ==========================================
        var compNetworking = await GetOrCreateCompany("IT Community & Mentorship", null, "BiH / Remote", "Professional Networking");
        var contEdin = CreateContact("Edin Salihagić", compNetworking.Id, "Software Engineer / Referrer", "edin.salihagic@gmail.com", null, null, ContactType.Referrer, "Preporuka za firme, slao CV kontaktima");
        var appEdin = CreateApplication(
            compNetworking,
            "Software Developer / Inženjer (Preporuke)",
            ApplicationStatus.Applied,
            new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc),
            ApplicationSource.Referral,
            "edin.salihagic@gmail.com",
            WorkMode.Remote,
            "Pisao edin.salihagic@gmail.com za preporuku nekim firmama on nekome slao ne znam kome",
            null);
        LinkApplicationContact(appEdin, contEdin);

        AddInteraction(appEdin, contEdin, InteractionDirection.Outbound, InteractionChannel.Email,
            new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc),
            "Molba za preporuku i uvid u CV – Edin Salihagić",
@"Pisao edin.salihagic@gmail.com

Za preorukau nekoim firmama on nekome slao ne znam kome

Ispod imaju moja dva CV-ja javno dostupna i na mom LinkedIn-u.

Link na CV: 
https://drive.google.com/drive/folders/13r56lFxyhHDBgArXek21Qprh_8Gtb-QQ?usp=sharing

Link na LinkedIn:
https://www.linkedin.com/in/nedim-jugo-492b99277?utm_source=share_via&utm_content=profile&utm_medium=member_android

Link na GitHub:
https://github.com/NedimJugo");

        // ==========================================
        // 4. ITO (Rejected)
        // ==========================================
        var compITO = await GetOrCreateCompany("ITO", "https://ito.dev", "Mostar, BiH", "Software Development");
        var contSanja = CreateContact("Sanja Zovko", compITO.Id, "HR Manager", "sanja@ito.ba", "+387 63 685 886", null, ContactType.Recruiter, "HR Manager ITO");
        var contMarijaB = CreateContact("MarijaB", compITO.Id, "Referrer", null, null, null, ContactType.Referrer, "Preporuka za firmu ITO");
        var appITO = CreateApplication(
            compITO,
            "Junior Software Developer",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 3, 10, 15, 0, DateTimeKind.Utc),
            ApplicationSource.Referral,
            "Preporuka od MarijaB, poslano na sanja@ito.ba",
            WorkMode.Onsite,
            "Čak za firmu dobio preporuku od MarijaB. Slao na sanja@ito.ba.",
            "Trenutno nismo u potrazi za nekim vaših kvaliteta.");
        LinkApplicationContact(appITO, contSanja);
        LinkApplicationContact(appITO, contMarijaB);

        AddInteraction(appITO, contSanja, InteractionDirection.Outbound, InteractionChannel.Email,
            new DateTime(2026, 9, 3, 10, 15, 0, DateTimeKind.Utc),
            "Prijava za poziciju u razvojnom timu ITO – Nedim Jugo",
@"Poštovanje,

Zovem se Nedim Jugo, diplomirani bachelor softverskog inženjeringa(Univerzitet „Džemal Bijedić“, Mostar), i javljam se putem ovim putem u potražnji za pozicijom u Vašem razvojnom timu.

Iskreno bi volio da ITO bude prva stanica u mojoj karijeri. Kao mostarska firma koja gradi konkretna softverska rješenja kako za sam grad tako i za veće i naprednije nivoe i klijente, teško mogu zamisliti bolje mjesto da započnem svoj profesionalni put kao developer.

U prilogu Vam šaljem svoj CV i motivaciono pismo, sa detaljnijim pregledom dosadašnjeg iskustva i projekata. Stojim na raspolaganju za razgovor u terminu koji vama odgovara.

Unaprijed zahvaljujem na vremenu i razmatranju moje prijave.

S poštovanjem,
Nedim Jugo
+387 60 318 5869
nedim.jugoo@gmail.com");

        AddInteraction(appITO, contSanja, InteractionDirection.Inbound, InteractionChannel.Email,
            new DateTime(2026, 9, 4, 11, 20, 0, DateTimeKind.Utc),
            "Odgovor Sanje Zovko (HR Manager) – Odbijenica",
@"Poštovani,

zahvaljujemo se na Vašem javljanju.

Trenutno nismo u potrazi za nekim vaših kvaliteta. Svakako vam želimo sve najbolje u vašem daljnjem radu.

Lijep pozdrav,

SANJA ZOVKO  | HR MANAGER
sanja@ito.ba  | +387 (0) 63 685 886  | +387 (0) 36 830 003  | ito.dev");

        // ==========================================
        // 5. GALEYO (Rejected)
        // ==========================================
        var compGaleyo = await GetOrCreateCompany("Galeyo", "https://galeyo.com", "Mostar, BiH", "Software Development");
        var contNinaH = CreateContact("Nina H.", compGaleyo.Id, "Head of Human Potential & Employer Branding", null, null, "https://www.linkedin.com/in/nina-h", ContactType.Recruiter);
        var appGaleyo = CreateApplication(
            compGaleyo,
            "Junior Software Developer",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 2, 14, 0, 0, DateTimeKind.Utc),
            ApplicationSource.CompanyWebsite,
            "General application + LinkedIn direct outreach",
            WorkMode.Onsite,
            "Poslao na general application. Pisao i Nini H. na LinkedIn.",
            "Trenutno nema pozicija za moj profil; CV sačuvan u bazi.");
        LinkApplicationContact(appGaleyo, contNinaH);

        AddInteraction(appGaleyo, null, InteractionDirection.Inbound, InteractionChannel.Email,
            new DateTime(2026, 9, 2, 14, 0, 0, DateTimeKind.Utc),
            "Galeyo - Potvrda prijave na General Application",
@"Dear Nedim Jugo,

Thank you for your successful application. If your application matches the job description we are looking for, our team will be happy to contact you.

Thank you for your application.

Best regards");

        AddInteraction(appGaleyo, contNinaH, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 5, 18, 9, 0, DateTimeKind.Utc),
            "LinkedIn poruka za Ninu H. (Subota, 18:09)",
@"Poštovanje, znam da je subota i da ne radite tako da ne očekujem tokom vikenda ni odgovor. Prvo bi volio da se predstavim, ja sam Nedim Jugo bachelor softverskog inženjeringa, trenutno sam u potrazi za poslom i po mogućnosti nekom početnom pozicijom koja bi mi dala iskustvo, priliku za učenje ali i mogućnost da sudjelujem sa timom u projektima i developmentu. Prije nekoliko dana kroz vašu formu na stranici sam poslao prijavu gdje sam unio svoje podatke, CV i motivaciono. Galeyo me privlači jer sam čuo samo dobre stavari o vašoj firmi, ali i također jer imate ured u Mostaru pa ne bi bio prinuđen na selidbu. Ono zbog čega vam se javljam jeste kako bi vidio da li ima mogućnosti da u sljedećoj sedmici pogledate moj CV i motivaciono i kolike su mogućnosti rada u vašim timovima. Ja bi svakako bio otvoren za bilo kakav razgovor u terminu kada vama odgovara. Nadam se da vam neće smetati to što vam se sada javljam i što vam se javljam ovim putem, ali unaprijed vam hvala na svakom odgovoru i pomoći.");

        AddInteraction(appGaleyo, contNinaH, InteractionDirection.Inbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 7, 10, 54, 0, DateTimeKind.Utc),
            "Odgovor Nine H. na LinkedIn-u (Ponedjeljak, 10:54)",
@"Pozdrav Nedime,

prije svega želim da se zahvalim na lijepim riječima i vašem interesu za kompaniju Galeyo. Mi ćemo svakako biti slobodni da vas pozovemo ukoliko se otvori neko radno mjesto koje bi odgovaralo vašem profesionalnom profilu.

Lijep pozdrav,
Nina");

        AddInteraction(appGaleyo, contNinaH, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 7, 11, 25, 0, DateTimeKind.Utc),
            "Odgovor Nedima Nini H. (Ponedjeljak, 11:25)",
@"Pozdrav i Vama,

Hvala Vam na razmatranju moje prijave. Ono što Vam ja mogu garatovati, ukoliko bude izabran za člana Vašeg tima, je prilagođavanje i poštovanje Vaših standarda i dodatni razvoj i rad. Žao mi je što trenutno ne postoji pozicija za moj profil, ali hvala Vama na javljanju i odgovoru, u svakom slučaju ostajem dostupan za daljnji razgovor i mogući intervju gdje ćete imati priliku vidjeti i upoznati mene kako u tehničkom tako i društvenom karakteru.

Ugodan dan Vam želim

Seen by Nina H. at 1:07 PM.");

        // ==========================================
        // 6. HTEC GROUP (Ghosted)
        // ==========================================
        var compHTEC = await GetOrCreateCompany("HTEC Group", "https://htecgroup.com", "Mostar, BiH", "Software Engineering");
        var contDamir = CreateContact("Damir Avdić", compHTEC.Id, "Embedded Software Engineer / PhD candidate", null, null, "https://www.linkedin.com/in/damir-avdic", ContactType.Peer, "Embedded software engineer, preporuke za HTEC");
        var contHTECOffice = CreateContact("HTEC Mostar Office", compHTEC.Id, "Office Mostar", "office-mo@htecgroup.com", null, null, ContactType.Recruiter);
        var appHTEC = CreateApplication(
            compHTEC,
            "Junior Software Developer / Junior Project Manager",
            ApplicationStatus.Ghosted,
            new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc),
            ApplicationSource.Other,
            "office-mo@htecgroup.com + LinkedIn Damir Avdić",
            WorkMode.Hybrid,
            "Slao na mejl u Mostaru niko nije nikad odgovorio. Javio se i Damiru na LinkedIn.",
            "Na mail u Mostaru niko nikad nije odgovorio; Damir Avdić potvrdio da za sada nema junior rola.");
        LinkApplicationContact(appHTEC, contDamir);
        LinkApplicationContact(appHTEC, contHTECOffice);

        AddInteraction(appHTEC, contHTECOffice, InteractionDirection.Outbound, InteractionChannel.Email,
            new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc),
            "Prijava na email HTEC office Mostar – Nedim Jugo",
@"Poštovani,

obraćam Vam se sa interesovanjem za mogućnosti zaposlenja u HTEC-u, odnosno u Vašem uredu u Mostaru.

Nedavno sam završio Bachelor studij Softverskog inženjeringa i trenutno tražim priliku za početak profesionalne karijere. Najviše se pronalazim u pozicijama Junior Software Developera ili Junior Project Managera, gdje bih mogao iskoristiti svoje tehničko znanje, iskustvo na projektima i interes za organizaciju i vođenje projekata.

Ipak, otvoren sam i za druge junior pozicije za koje smatrate da bi odgovarale mom profilu i vještinama. Rado bih razgovarao o dostupnim mogućnostima i čuo više o tome gdje bih mogao najbolje doprinijeti Vašem timu, ali i naučiti nove stvari od Vaših mentora.

U prilogu dostavljam svoj CV i motivaciono pismo, a ukoliko postoji odgovarajuća prilika, bio bih zahvalan za mogućnost kratkog razgovora.

Hvala Vam na vremenu i razmatranju moje prijave.

S poštovanjem,
Nedim Jugo
+387 60 318 5869
nedim.jugoo@gmail.com");

        AddInteraction(appHTEC, contDamir, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 4, 15, 21, 0, DateTimeKind.Utc),
            "Poruka Damiru Avdiću (3:21 PM)",
@"Pozdrav Damire, dok sam listao LinkedIn imao sam priliku naići na vašu nedavnu objavu o zapošljavanju u HTEC-u i da ste se ponudili da budete taj gateway da TA, pa nadam se da vam inbox nije pun al u nadi da ćete vidjeti moju poruku. Ukratko završio sam 4. godine faksa i softverski inženjer sam, sad tražim neku početnu poziciju kako bi krenuo sa karijerom. Prije nekoliko dana na vas HTEC office Mostar sam poslao svoj CV i motivaciono pismo kao nadu da ću možda uspjeti dobiti neku poziciju. Ono što mene zanima je da li biste bili zainteresovani za neke pozicije juniora i koliko je to moguće u Mostaru i ako ""da"" biste li mogli nekome možda sugerisati da pogleda moj mejl. Naravno unaprijed vam hvala");

        AddInteraction(appHTEC, contDamir, InteractionDirection.Inbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 4, 15, 34, 0, DateTimeKind.Utc),
            "Odgovor Damira Avdića (3:34 PM)",
@"Pozdrav Nedime,
Za sada je otvoreno samo to sto je na webu, mislim da junior rola nema ali to je samo trenutno.
Vjerujem da su kolege zaprimile CV i motivaciono i da ako se ukaze pozicija da ce te zvati.
Prati web, i kanale HTECa ako se nesto otvori gdje mozes konkurisati - pingaj mene pa cu ja da vidim sta je sa CV-jem.");

        AddInteraction(appHTEC, contDamir, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 4, 15, 36, 0, DateTimeKind.Utc),
            "Odgovor Nedima Damiru (3:36 PM)",
@"Hvala svakako na javljanju. Pratit ću stranice, MSM kao što i do sad radim. Vidim da su prilike za nas male ali eto mislio sam svakako da vrijedi pokušati poslati a i napisati Vama pa šta bude bude");

        AddInteraction(appHTEC, contDamir, InteractionDirection.Inbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 4, 15, 39, 0, DateTimeKind.Utc),
            "Odgovor Damira Avdića (3:39 PM)",
@"A nazalost znam da je problem juniorima u danasnje vrijeme jer je kriza pogodila outsourcing kompanije a ondao dosao i AI. 
Trebalo bi da se ustabili do kraja godine, probaj i na Linkedinu da filtriras poslove i da vidis gdje mozes upast.
Bolje ti je javi se direktno TA/Recruiting timu na Linkedinu nego da saljes CV :)");

        AddInteraction(appHTEC, contDamir, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 4, 15, 43, 0, DateTimeKind.Utc),
            "Odgovor Nedima Damiru (3:43 PM)",
@"Svakako i to radim javljam se al nekako sad za sad ima dva mjeseca samo nailazim na odgovore nema. Al opet nije ovo vas problem pa da slušate a ako vi imali neku poziciju i nešto ako se slučajno sjetite mene ja sam dostupan");

        AddInteraction(appHTEC, contDamir, InteractionDirection.Inbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 4, 15, 47, 0, DateTimeKind.Utc),
            "Odgovor Damira Avdića (3:47 PM)",
@"Ja sam se zaposljavao u zlatnom dobu ITa pa sam 4-5 mjeseci trazio posao, cisto da znas da nije tako crno, doci ce :)");

        AddInteraction(appHTEC, contDamir, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 4, 16, 0, 0, DateTimeKind.Utc),
            "Zahvala Damiru Avdiću (4:00 PM)",
            "Hvala na motivaciji i lijepim rijecima");

        // ==========================================
        // 7. SAAS SOLUTIONS (Rejected)
        // ==========================================
        var compSaaS = await GetOrCreateCompany("SaaS Solutions", "https://saasolutions.ba", "Mostar, BiH", "Software Development");
        var contEmina = CreateContact("Emina Ćehajić", compSaaS.Id, "Head of HR SaaS Solutions, powered by mih GmbH", null, null, null, ContactType.Recruiter);
        var contMateja = CreateContact("Mateja Šumić", compSaaS.Id, "IT Recruiter at SQA Consulting", "mateja.sumic@sqa-consulting.com", null, null, ContactType.Recruiter);
        var contNihad = CreateContact("Nihad (Direktor SaaS Solutions)", compSaaS.Id, "Direktor", null, null, null, ContactType.HiringManager, "Direktor predstavio firmu na FITCC");
        var appSaaS = CreateApplication(
            compSaaS,
            "Junior Software Developer / Junior Project Manager",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 2, 17, 50, 0, DateTimeKind.Utc),
            ApplicationSource.Other,
            "careers@saasolutions.ba + LinkedIn Emina Ćehajić",
            WorkMode.Onsite,
            "Slao na mejl niko nije odgovorio. Javio se Emini na LinkedIn koja me uputila na Mateju Šumić.",
            "Trenutno nemaju otvorenih pozicija za junior developere; CV sačuvan u bazi.");
        LinkApplicationContact(appSaaS, contEmina);
        LinkApplicationContact(appSaaS, contMateja);
        LinkApplicationContact(appSaaS, contNihad);

        AddInteraction(appSaaS, null, InteractionDirection.Outbound, InteractionChannel.Email,
            new DateTime(2026, 9, 2, 17, 50, 0, DateTimeKind.Utc),
            "Prijava za junior pozicije – SaaS Solutions (careers@saasolutions.ba)",
@"Poštovani,

obraćam Vam se sa željom da se prijavim za neku od dostupnih junior pozicija u SaaS Solutions.

Nedavno sam završio Bachelor studij Softverskog inženjerstva i trenutno tražim priliku za početak profesionalne karijere. Posebno me zanimaju pozicije vezane za software development i projekt menadžment, ali sam otvoren i za druge pozicije koje odgovaraju mom znanju, iskustvu i interesovanjima.

Vidjevši da ste otvorili ured u Mostaru, posebno mi je bilo interesantno da istražim mogućnost pridruživanja Vašem timu upravo na ovoj lokaciji.

Prijavu šaljem putem e-maila jer sam želio dostaviti i motivaciono pismo, što nisam bio u mogućnosti učiniti putem online forme. U prilogu se nalaze moj CV i motivaciono pismo.

Bio bih veoma zainteresovan za priliku da razgovaramo o eventualnim otvorenim pozicijama i mogućnostima za moj profil. Spreman sam razmotriti i druge uloge ukoliko smatrate da bih se bolje uklopio u neku drugu poziciju u Vašem timu.

Hvala Vam na vremenu i razmatranju moje prijave.

S poštovanjem,
Nedim Jugo
+387 60 318 5869
nedim.jugoo@gmail.com");

        AddInteraction(appSaaS, contEmina, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 10, 13, 26, 0, DateTimeKind.Utc),
            "LinkedIn poruka Emini Ćehajić (1:26 PM)",
@"Poštovanje Emina,

Nadam se da vam ne smeta što vam se javljam ovdje, prije nekoliko dana sam na mejl careers@saasolutions.ba poslao samoonicijativnu prijavu za junior dev poziciju u SaaSSolutions, pa bi i ovom prilikom htio malo objasniti zašto mi se baš vaša kompanija čini kao dobar start i boost, ali i možda tražiti feedback ako je to moguće. 

Pošto živim u Mostaru i prije nekoliko mjeseci vidio sam da imate office i ovdje, što mi je bilo lijepo iznenađenje. Prošle godine sam imao priliku porazgovarati s Vašim direktorom gospodinom Nihadom na FITCC-u gdje mi je on no predstavio kompaniju i tehnologije, i tom prilikom sam primijetio koliko se tehnologije s kojima radite poklapaju s onim na čemu ja radim - ASP.NET Core, Angular, React, SQL Server, plus nekoliko full-stack projekata koje sam sam vodio od ideje do produkcije.

Bilo bi mi drago ako biste mogli pogledati moju prijavu kad budete imali priliku i javiti mi ima li trenutno u Vašem timu prostora za nekoga s mojim profilom. Ono što ja još mogu garantovati je moje puno prilagođavanje Vašim standardima, ali i vidim tu mogućnost mog daljeg učenja i napredovanja. Otvoren sam za razgovor kad god Vama odgovara. 

Hvala Vam puno i lijep pozdrav,
Nedim");

        AddInteraction(appSaaS, contEmina, InteractionDirection.Inbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 10, 14, 23, 0, DateTimeKind.Utc),
            "Odgovor Emine Ćehajić (2:23 PM)",
@"Dragi Nedime, hvala na javljanju i interesu za moguću saradnju! Zamolila bih te da se javiš kolegici Mateji Sumic koja je u HR regrutacijskim procesima.
Svako dobro!");

        AddInteraction(appSaaS, contEmina, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 10, 14, 55, 0, DateTimeKind.Utc),
            "Odgovor Nedima Emini (2:55 PM)",
            "Mnogo hvala na pročitanoj poruci i odgovoru, poslao sam zahtjev Vašoj kolegici i pišem uskoro. Ugodan dan");

        AddInteraction(appSaaS, contEmina, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 14, 10, 55, 0, DateTimeKind.Utc),
            "Upit Emini za email kontakt Mateje Šumić (10:55 AM)",
@"Poštovanje, izvinjavam se što se ponovo javljam. Vašoj kolegici sam u četvrtak poslao zahtjev na LinkedIn-u, ali pošto mi nije još odobrila nisam joj u mogućnosti napisati poruku. Zanima me samo znate li slučajno da kolegica nije na odmoru ili ako vi imate bolji način da je kontaktiram.");

        AddInteraction(appSaaS, contEmina, InteractionDirection.Inbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 14, 11, 35, 0, DateTimeKind.Utc),
            "Odgovor Emine Ćehajić sa email adresom (11:35 AM)",
@"Cao cao, saljem mail;)
mateja.sumic@sqa-consulting.com");

        AddInteraction(appSaaS, contEmina, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 14, 11, 44, 0, DateTimeKind.Utc),
            "Zahvala Emini (11:44 AM)",
            "Hvala ponovo, mejl već poslan");

        AddInteraction(appSaaS, contMateja, InteractionDirection.Outbound, InteractionChannel.Email,
            new DateTime(2026, 9, 14, 11, 43, 0, DateTimeKind.Utc),
            "Prijava za posao – Mateja Šumić (SQA Consulting)",
@"Poštovanje Mateja,

U četvrtak sam se javio Vašoj kolegici Emini Ćehajić koja me uputila da se obratim Vama vezano za moju prijavu - prije nekoliko dana sam na mejl careers@saasolutions.ba poslao samoinicijativnu prijavu za junior dev poziciju u SaaSSolutions, pa bih i ovom prilikom htio malo objasniti zašto mi se baš vaša kompanija čini kao dobar start i boost, ali i možda tražiti feedback ako je to moguće.

Pošto živim u Mostaru i prije nekoliko mjeseci vidio sam da imate office i ovdje, što mi je bilo lijepo iznenađenje. Prošle godine sam imao priliku porazgovarati s Vašim direktorom gospodinom Nihadom na FITCC-u gdje mi je on predstavio kompaniju i tehnologije, i tom prilikom sam primijetio koliko se tehnologije s kojima radite poklapaju s onim na čemu ja radim - ASP.NET Core, Angular, React, SQL Server, plus nekoliko full-stack projekata koje sam sam vodio od ideje do produkcije.

Bilo bi mi drago ako biste mogli pogledati moju prijavu kad budete imali priliku i javiti mi ima li trenutno u Vašem timu prostora za nekoga s mojim profilom. Ono što ja još mogu garantovati je moje puno prilagođavanje Vašim standardima, ali i vidim tu mogućnost mog daljeg učenja i napredovanja. Otvoren sam za razgovor kad god Vama odgovara. Ukoliko je potrebno da i ovdje pošaljem CV i motivaciono nije nikakav problem.

Hvala Vam puno i lijep pozdrav,
Nedim");

        AddInteraction(appSaaS, contMateja, InteractionDirection.Inbound, InteractionChannel.Email,
            new DateTime(2026, 9, 15, 12, 49, 0, DateTimeKind.Utc),
            "Odgovor Mateje Šumić (12:49 PM)",
@"Pozdrav Nedim,

Hvala Vam puno na javljanju i Vašem interesu za našu kompaniju. 
Nažalost trenutno nemamo otvorenih pozicija za junior developere, ali bismo svakako željeli da imamo Vaš CV u našoj bazi ako se situacija promijeni i otvori se neka pozicija na koju biste nam se uklapali.

Stoga Vas molim ako možete da pošaljete Vaš CV. 

Hvala Vam još jednom na interesu za našu kompaniju. 

Srdačan pozdrav,

Mateja Šumić
IT Recruiter");

        AddInteraction(appSaaS, contMateja, InteractionDirection.Outbound, InteractionChannel.Email,
            new DateTime(2026, 9, 15, 12, 54, 0, DateTimeKind.Utc),
            "Slanje CV-a Mateji Šumić (12:54 PM)",
@"Poštovana Mateja,

Hvala Vam na odgovoru.

U prilogu Vam dostavljam svoj CV. Iako razumijem da trenutno nema otvorenih pozicija za junior developere, volio bih Vas zamoliti da svakako pogledate moj profil kada budete u prilici.

Izuzetno sam motivisan za rad u Vašem timu, posebno u mostarskom uredu, te sam potpuno otvoren i voljan razmotriti bilo koje druge uloge ili prilike (uključujući srodne tehničke pozicije ili projekt menadžment) ukoliko procijenite da bih svojim vještinama i profilom mogao odgovarati trenutnim ili skorim potrebama.

Stojim Vam na raspolaganju za kratak razgovor ili bilo kakve dodatne informacije.

Hvala Vam još jednom na izdvojenom vremenu.

Srdačan pozdrav,
Nedim Jugo
+387 60 318 5869
nedim.jugoo@gmail.com");

        AddInteraction(appSaaS, contMateja, InteractionDirection.Inbound, InteractionChannel.Email,
            new DateTime(2026, 9, 15, 13, 21, 0, DateTimeKind.Utc),
            "Potvrda prijema CV-a od Mateje Šumić (1:21 PM)",
@"Pozdrav Nedim,

Nema na čemu, hvala Vama na brzom odgovoru i slanju. 
Pogledamo svakako Vaš CV detaljnije, te Vas kontaktiramo ako se otvori neka prilika.

Hvala Vam još jednom i bit ćemo u kontaktu za dalje.

Srdačan pozdrav,
Mateja Šumić
IT Recruiter");

        // ==========================================
        // 8. SOFTRAY SOLUTIONS (Ghosted)
        // ==========================================
        var compSoftray = await GetOrCreateCompany("Softray Solutions", "https://www.softraysolutions.com", "Sarajevo, BiH", "Software Development");
        var contSoftray = CreateContact("Softray Careers", compSoftray.Id, "HR Department", "careers@softraysolutions.com", null, null, ContactType.Recruiter);
        var appSoftray = CreateApplication(
            compSoftray,
            "Junior Software Developer",
            ApplicationStatus.Ghosted,
            new DateTime(2026, 9, 4, 16, 38, 0, DateTimeKind.Utc),
            ApplicationSource.Other,
            "careers@softraysolutions.com",
            WorkMode.Hybrid,
            "Slao na general application careers@softraysolutions.com. Dobio samo automatizirani mejl.",
            "Dobio samo automatizirani mejl, niko se poslije nije javio.");
        LinkApplicationContact(appSoftray, contSoftray);

        AddInteraction(appSoftray, contSoftray, InteractionDirection.Outbound, InteractionChannel.Email,
            new DateTime(2026, 9, 4, 16, 38, 0, DateTimeKind.Utc),
            "Otvorena aplikacija za poziciju u razvojnom timu – Nedim Jugo",
@"Poštovani,

Zovem se Nedim Jugo, diplomirani inženjer softvera (Univerzitet „Džemal Bijedić“, Mostar), i javljam se putem otvorene aplikacije za poziciju u vašem razvojnom timu u Sarajevu.

Do sada sam radio na nekoliko projekata u tehnologijama koje i vi koristite - Angular, ASP.NET Core, Azure - pa smatram da bih se brzo uklopio u tim i počeo doprinositi projektima za vaše klijente, ali i da bi Vaša ekipa mogla pomoći meni da bolje radim i više naučim.

U prilogu vam šaljem CV i motivaciono pismo, sa detaljnijim pregledom dosadašnjeg iskustva i projekata. Stojim na raspolaganju za razgovor u terminu koji vama odgovara.

Unaprijed zahvaljujem na vremenu i razmatranju moje prijave.

S poštovanjem,
Nedim Jugo
+387 60 318 5869
nedim.jugoo@gmail.com");

        AddInteraction(appSoftray, contSoftray, InteractionDirection.Inbound, InteractionChannel.Email,
            new DateTime(2026, 9, 4, 16, 38, 0, DateTimeKind.Utc),
            "Automatski odgovor – Softray Careers",
@"Thank you for expressing an interest in employment with Softray Solutions. Our HR department will review your application and get back to you in case you are selected to continue to the interview process.

If you do not hear from us, it is likely that we have identified a candidate for the position or the position is no longer active. Softray Solutions greatly appreciates your interest and would like to encourage you to periodically refer to www.softraysolutions.com/careers for the list of our current openings.

Thank you for your interest and we wish you success in your job search.

Regards,

Softray Team
www.softraysolutions.com");

        // ==========================================
        // 9. ZIRA GROUP (Rejected)
        // ==========================================
        var compZIRA = await GetOrCreateCompany("ZIRA Group", "https://ziragroup.com", "Sarajevo, BiH", "B2B Telecom & Fintech SaaS");
        var contArmin = CreateContact("Armin Babović", compZIRA.Id, "Talent Acquisition Specialist", "armin.babovic@ziragroup.com", null, null, ContactType.Recruiter);
        var contNedimS = CreateContact("Nedim Salahović", compZIRA.Id, "People Partner", null, null, null, ContactType.Recruiter);
        var contMedina = CreateContact("Medina", compZIRA.Id, "Recruiting Team", null, null, null, ContactType.Recruiter, "Kontakt za ZTA program");
        var appZIRA = CreateApplication(
            compZIRA,
            "Junior Software Developer / Junior Project Manager",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 3, 14, 0, 0, DateTimeKind.Utc),
            ApplicationSource.CompanyWebsite,
            "Open Application form + LinkedIn",
            WorkMode.Hybrid,
            "Javio se preko stranice forme, dobio automatizirani mejl. Pisao Nedimu Salahoviću pa Arminu Baboviću na LinkedIn.",
            "Trenutno nema direktnih otvorenih pozicija za junior developere; preusmjeren na prijavu za Dev ZTA internship.");
        LinkApplicationContact(appZIRA, contArmin);
        LinkApplicationContact(appZIRA, contNedimS);
        LinkApplicationContact(appZIRA, contMedina);

        AddInteraction(appZIRA, contArmin, InteractionDirection.Inbound, InteractionChannel.Email,
            new DateTime(2026, 9, 3, 14, 0, 0, DateTimeKind.Utc),
            "Potvrda prijave preko Open Application forme – ZIRA Group",
@"Dear Nedim.

Thank you for submitting your application and for considering ZIRA as part of your career journey.

We know that every application represents much more than a CV.

Behind it are years of learning, professional experiences, personal ambitions, career goals, and often the hope that the next opportunity could lead to something meaningful. That's why we never take an application for granted, and we're grateful that you chose to share yours with us.

What happens next?

You've applied through our Open Application form.

This means your profile will be available to our recruitment team for future opportunities. Because this is not an application for a specific open position, there may or may not be a role that matches your experience and aspirations at this moment. 

However, if an opportunity arises that aligns with your profile, we'll be happy to reach out and continue the conversation.

Why we keep open applications?

Some people apply because they are actively searching for their next role.

Others apply because they are curious about what the future might bring.

Both reasons matter.

At ZIRA, we value the opportunity to connect with people at every stage of their career journey. Whether you're just beginning to build your professional path or bringing years of experience and specialized expertise, we believe every conversation has the potential to create value.
While we don't always have an immediate opportunity for every profile, we've learned that meaningful connections often begin long before a specific position exists.

A small reminder.

Job searching can be exciting.

It can also be frustrating, uncertain, and sometimes much slower than we'd like.

If you're currently exploring new opportunities, we hope you'll remember that careers are rarely built through a single application or a single decision. They are built through persistence, continuous learning, curiosity, and the willingness to keep moving forward when progress isn't immediately visible.

The fact that you're investing in yourself and taking action already says a lot about your ambition, resilience, and commitment to growth.

Don't underestimate that.

At ZIRA, we believe that every interaction should leave something valuable behind.

The core of what we do is connection, and our goal is to create value wherever we can, whether through technology, knowledge, or simply a thoughtful human interaction.

That is why you can feel free to reach out to our Talent Acquisition Specialist for advice on job search or anything related to the job market.

Thank you once again for placing your trust in us.

Whether our paths cross in the near future or further down the road, we're grateful for the opportunity to get to know you.

And even if we never end up working together, we hope that our paths crossing today contributes in some small way to your professional journey.

Best regards,
Armin Babović
Talent Acquisition Specialist
ZIRA Group
LinkedIn");

        AddInteraction(appZIRA, contNedimS, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 15, 10, 41, 0, DateTimeKind.Utc),
            "Poruka Nedimu Salahoviću (10:41 AM)",
@"Poštovanje Nedime,

Nadam se da ste dobro i da Vam ne smeta inicijativa što Vam pišem ovim putem. Javljam Vam se jer već duže vrijeme pratim rad Zire, od FIT-CCa i priča sa Vašim ljudima pa sve do danas kad slavite 30 godina, a nekoliko kolega i kolegica koji rade kod Vas prenijeli su mi izuzetno pozitivna iskustva o atmosferi, timu i radnoj kulturi.

Završio sam softverski inžinjering na FIT-u u Mostaru. Kroz fakultet, praktičan rad i nagrađivane projekte na takmičenjima, stekao sam čvrste osnove u full-stack razvoju (.NET, C#,SQL, Angular, React, Flutter). Također, kroz vođenje timskih projekata i organizaciju različitih inicijativa razvio sam izražene organizacijske i komunikacijske vještine, pa me podjednako privlače i razvoj softvera i koordinacija projekata.

Već sam poslao svoj CV i motivaciono pismo putem Vaše zvanične forme za prijavu. Znam da je i teško i nezahvalno kao prvi posao tražiti specifične pozicije, ali bitno je da znate da se najkomotnije osjećam na pozicijama Junior Software Developera ili Junior Project Managera (PM), ali sam potpuno otvoren i za druge juniorske prilike ili Vaše prijedloge ukoliko smatrate da bih se negdje drugo bolje uklopio.

Bio bih Vam zahvalan ako ugrabite malo vremena da pogledate moju prijavu, a rado bih se i detaljnije predstavio ukoliko bude prilike za razgovor.

Hvala Vam na vremenu i lijep pozdrav!");

        AddInteraction(appZIRA, contNedimS, InteractionDirection.Inbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 18, 11, 19, 0, DateTimeKind.Utc),
            "Odgovor Nedima Salahovića (11:19 AM)",
@"Pozdrav Nedime, izvinjavam se na zakašnjelom odgovoru, nekako sam propustio poruku :) molim Vas, javite se našem TAS kolegi Arminu Baboviću, možete i na LinkedInu, možete i putem maila armin.babovic@ziragroup.com; on će biti u prilici da Vam da najkonkretnije informacije. 

Puno pozdrava i sreće!");

        AddInteraction(appZIRA, contNedimS, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 18, 11, 46, 0, DateTimeKind.Utc),
            "Zahvala Nedimu Salahoviću (11:46 AM)",
@"Zdravo Nedime, nema veze što je zakasnio malo ali makar je odgovor. Ja sam svakako Vašem kolegi poslao connection request na LinkedIn pa mi šaljem i poruku kad prihvati, a ako je negdje oko Vas bio bi zahvalan samo da mu napomenete da prihvati request samo kako bi komunikacija tekla brzim tokom. A ukoliko nto ne bude u nekih 1h vremena svakako mu šaljem i mejl. Hvala svakako na infomracijama");

        AddInteraction(appZIRA, contArmin, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 18, 11, 52, 0, DateTimeKind.Utc),
            "Pitanje/prijava za posao – Armin Babović (11:52 AM)",
@"Pitanje/prijava za posao
Poštovanje Armine,

Pisao sam Vašem kolegi Nedimu prije nekoliko dana, a on me danas uputio da se javim Vama. Nadam se da ste dobro i da Vam ne smeta inicijativa što Vam pišem ovim putem. Javljam Vam se jer već duže vrijeme pratim rad Zire, od FIT-CCa i priča sa Vašim ljudima pa sve do danas kad slavite 30 godina, a nekoliko kolega i kolegica koji rade kod Vas prenijeli su mi izuzetno pozitivna iskustva o atmosferi, timu i radnoj kulturi.

Završio sam softverski inžinjering na FIT-u u Mostaru. Kroz fakultet, praktičan rad i nagrađivane projekte na takmičenjima, stekao sam čvrste osnove u full-stack razvoju (.NET, C#,SQL, Angular, React, Flutter). Također, kroz vođenje timskih projekata i organizaciju različitih inicijativa razvio sam izražene organizacijske i komunikacijske vještine, pa me podjednako privlače i razvoj softvera i koordinacija projekata.

Već sam poslao svoj CV i motivaciono pismo putem Vaše zvanične forme za prijavu. Znam da je i teško i nezahvalno kao prvi posao tražiti specifične pozicije, ali bitno je da znate da se najkomotnije osjećam na pozicijama Junior Software Developera ili Junior Project Managera (PM), ali sam potpuno otvoren i za druge juniorske prilike ili Vaše prijedloge ukoliko smatrate da bih se negdje drugo bolje uklopio.

Bio bih Vam zahvalan ako ugrabite malo vremena da pogledate moju prijavu, a rado bih se i detaljnije predstavio ukoliko bude prilike za razgovor.

Hvala Vam na vremenu i lijep pozdrav!");

        AddInteraction(appZIRA, contArmin, InteractionDirection.Inbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 22, 16, 2, 0, DateTimeKind.Utc),
            "Odgovor Armina Babovića (4:02 PM)",
@"Pozdrav Nedime, drago mi je da si pisao.

Trenutno nemamo otvorenih pozicija, ali možeš se prijaviti na Dev ZTA koji će uskoro biti aktivan.

Mislim da je tako najbolje, i onda će kolege da procijene tvoj profil i ako sve bude okej zvat će te. To je internship koji može zaposliti zaposlenjem u ZIRI.

Na taj način možeš krenuti kao software developer");

        AddInteraction(appZIRA, contArmin, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 22, 16, 28, 0, DateTimeKind.Utc),
            "Odgovor Nedima Arminu (4:28 PM)",
@"Hvala na javljanju Armine, žao mi je što u vašim timovima nema mjesta za jednog juniora, ali hvala na informaciji. Pratit ću svakako i ZTA, a trenutno sam stvarno u potrazi za nekim permanentnim zaposlenjem, jer onako kad završiš faks vrijeme je za takvo nešto.");

        AddInteraction(appZIRA, contArmin, InteractionDirection.Inbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 22, 16, 33, 0, DateTimeKind.Utc),
            "Odgovor Armina Babovića o ZTA programu (4:33 PM)",
@"Pa ima mjesta kroz taj ZTA, tako se kod nas zapošljavaju juniori.

Tako da, dobro spremi prijavu, javi se slobodno Medini iz našeg tima, pošalji neke svoje projekte.

Svi juniori u firmi dolaze kroz ZTA, ima 3 mjeseca praktičnog rada i onda se na kraju dolazi do odluke ko će ostati kao stalni član tima.");

        AddInteraction(appZIRA, contArmin, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 22, 16, 53, 0, DateTimeKind.Utc),
            "Odgovor Nedima Arminu Baboviću (4:53 PM)",
@"Hvala na preporukama, prijavim se na ZTA pa vidim kako i to prođe.

Seen by Armin Babović at 4:54 PM.");

        // ==========================================
        // 10. MANPOWER BOSNA I HERCEGOVINA (Applied)
        // ==========================================
        var compManpower = await GetOrCreateCompany("Manpower Bosna i Hercegovina", "https://manpower.ba", "Sarajevo, BiH", "Recruitment Agency");
        var contManpower = CreateContact("Manpower BiH", compManpower.Id, "Talent Acquisition", "noreply@manpowersee.com", null, null, ContactType.Recruiter);
        var appManpower = CreateApplication(
            compManpower,
            "Call for IT professionals",
            ApplicationStatus.Applied,
            new DateTime(2026, 9, 4, 18, 10, 0, DateTimeKind.Utc),
            ApplicationSource.Referral,
            "noreply@manpowersee.com",
            WorkMode.Hybrid,
            "Poslao ovdje prijavu u IT bazu oni nisu firma nego vise kao ako bude posla zovnu.",
            null);
        LinkApplicationContact(appManpower, contManpower);

        AddInteraction(appManpower, contManpower, InteractionDirection.Inbound, InteractionChannel.Email,
            new DateTime(2026, 9, 4, 18, 10, 0, DateTimeKind.Utc),
            "Potvrda prijave: Call for IT professionals",
@"Pozdrav Nedim Jugo!

Manpower Bosna i Hercegovina Vam se zahvaljuje na prijavi za poziciju:
Call for IT professionals
U nastavku su podaci koje ste unijeli u obrazac za prijavu:

Ime i prezime: Nedim Jugo
E-mail adresa: nedim.jugoo@gmail.com
Kontakt telefon: 0603185869
Koliko godina relevantnog iskustva imate za poziciju na koju se prijavljujete?: 1
Koja su Vaša finansijska očekivanja?: 1750
Da li ste spremni za relokaciju?: Ne
Koliki je Vaš otkazni rok?: 8
Saglasnost na obradu podataka: Da, Vrijeme: 2026-09-04 18:08

Pregledat ćemo Vašu prijavu i kontaktirati Vas ukoliko nam budu potrebne dodatne informacije.");

        // ==========================================
        // 11. BH TELECOM (Rejected)
        // ==========================================
        var compBHTelecom = await GetOrCreateCompany("BH Telecom d.d. Sarajevo", "https://www.bhtelecom.ba", "Sarajevo, BiH", "Telecommunications");
        var contBHTelecomHR = CreateContact("BH Telecom Ljudski Resursi", compBHTelecom.Id, "Izvršna direkcija za pravne poslove i ljudske resurse", "ljudski.resursi@bhtelecom.ba", null, null, ContactType.Recruiter);
        var contSamir = CreateContact("Samir Omerović", compBHTelecom.Id, "HR Specialist", "Samir.Omerovic@bhtelecom.ba", null, null, ContactType.Recruiter);
        var contAmila = CreateContact("Amila Merdžanović", compBHTelecom.Id, "HR Specialist", "Amila.Merdzanovic@bhtelecom.ba", null, null, ContactType.Recruiter);
        var appBHTelecom = CreateApplication(
            compBHTelecom,
            "Junior Developer / IT pozicija",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 6, 10, 0, 0, DateTimeKind.Utc),
            ApplicationSource.CompanyWebsite,
            "ljudski.resursi@bhtelecom.ba",
            WorkMode.Onsite,
            "Javio se preko stranice, dobio odgovor o proceduri javnog oglašavanja i privremenoj obustavi konkursa.",
            "Prijem isključivo putem javnog oglašavanja po Uredbi Vlade FBiH; postupci privremeno obustavljeni Zaključkom Vlade FBiH.");
        LinkApplicationContact(appBHTelecom, contBHTelecomHR);
        LinkApplicationContact(appBHTelecom, contSamir);
        LinkApplicationContact(appBHTelecom, contAmila);

        AddInteraction(appBHTelecom, contBHTelecomHR, InteractionDirection.Inbound, InteractionChannel.Email,
            new DateTime(2026, 9, 7, 10, 33, 0, DateTimeKind.Utc),
            "RE: Aplikacija za posao - Nedim Jugo (BH Telecom)",
@"Poštovani,

Zahvaljujemo se na Vašoj molbi za posao dostavljenoj našoj kompaniji. Prijem novog radnika na neodređeno vrijeme u Dioničkom društvu BH Telecom Sarajevo vrši se isključivo putem javnog oglašavanja, a cijeneći činjenicu da je naša kompanija obveznik primjene Uredbe o postupku prijema u radni odnos u javnom sektoru Federacije Bosne i Hercegovine. S tim u vezi, ovim putem Vas pozivamo da u sredstvima javnog informisanja, kao i na našoj web stranici https://www.bhtelecom.ba/karijera/ pratite objavljivanje oglasa za popunu radnih mjesta za koja se traži stručna sprema i znanje koje posjedujete, te da ukoliko budete zainteresovani podnesete prijavu u skladu sa uslovima iz javnog oglasa.

Također napominjemo da će Vaša molba biti evidentirana u registar zaprimljenih aplikacija potencijalnih kandidata za zasnivanje radnog odnosa i bit uzeta u razmatranje ukoliko se u Dioničkom društvu BH Telecom Sarajevo ukaže potreba za prijemom radnika na određeno vrijeme, do 6 (šest) mjeseci, a da znanje i vještine koje posjedujete odgovaraju profilu kandidata kojeg tražimo.

Ukoliko ste to propustili učiniti, pozivamo Vas da nam dostavite Vaš CV koji sadrži podatke i o području rada koje Vas interesuje putem e-maila: ljudski.resursi@bhtelecom.ba.

Ujedno koristimo priliku da Vas informišemo da su Zaključkom Vlade Federacije Bosne i Hercegovine privremeno obustavljeni postupci prijema u radni odnos, uz izuzetak onih koje odobri resorno ministarstvo.

Za sva eventualna pitanja, budite slobodni da nas kontaktirate putem gornje e-mail adrese.

S poštovanjem,

LJUDSKI RESURSI
www.bhtelecom.ba

Izvršna direkcija za pravne poslove, upravljanje organizacijom
I ljudskim resursima
BH Telecom d.d. Sarajevo
Franca Lehara br. 7
71 000 Sarajevo
e: ljudski.resursi@bhtelecom.ba");

        // ==========================================
        // 12. ENDAVA (Rejected)
        // ==========================================
        var compEndava = await GetOrCreateCompany("Endava", "https://www.endava.com", "Mostar / Sarajevo, BiH", "Software Engineering");
        var contBedirhan = CreateContact("Bedirhan Ramiz", compEndava.Id, "Talent Acquisition Specialist", "Bedirhan.Ramiz@endava.com", "+38761338932", null, ContactType.Recruiter);
        var appEndava = CreateApplication(
            compEndava,
            "Junior Developer",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 6, 13, 1, 0, DateTimeKind.Utc),
            ApplicationSource.Other,
            "endava.recruitment.bih@endava.com",
            WorkMode.Hybrid,
            "Javio se na mail endava.recruitment.bih@endava.com. Dobio odgovor od Bedirhana Ramiza.",
            "Trenutno nemamo prostora za neke nove kolege u našoj zemlji.");
        LinkApplicationContact(appEndava, contBedirhan);

        AddInteraction(appEndava, contBedirhan, InteractionDirection.Outbound, InteractionChannel.Email,
            new DateTime(2026, 9, 6, 13, 1, 0, DateTimeKind.Utc),
            "Prijava za poziciju Junior Developer – Nedim Jugo",
@"Poštovani,

Zovem se Nedim Jugo i diplomirani sam bachelor softverskog inženjeringa (Fakultet informacijskih tehnologija, Univerzitet „Džemal Bijedić“ u Mostaru). Obraćam vam se sa željom da svoju karijeru započnem kao Junior Developer u vašem timu u Mostaru.

Tokom studija i rada na praktičnim projektima fokusirao sam se na razvoj cjelovitih softverskih rješenja:

Backend & API: Razvoj skalabilnih servisa i baza podataka (.NET / C#, SQL Server, REST API, arhitektura zasnovana na mikroservisima/porukama).

Frontend & Mobile: Izrada modernih web i cross-platform aplikacija (Angular, React, Next.js, Flutter).

Integracije i primijenjeni AI: Praktično iskustvo sa integracijom pametnih servisa, računarskog vida (CV) i IoT/embedded hardverskih rješenja.

Kroz samostalan i timski rad na kompleksnim sistemima navikao sam na brz ulazak u novu tehnologiju, rješavanje konkretnih inženjerskih problema i pisanje čistog, održivog koda.

U prilogu vam dostavljam svoj CV i motivaciono pismo (na bosanskom i engleskom jeziku), u kojima se nalazi detaljniji pregled mojih projekata, koda i dosadašnjeg iskustva.

Rado bih porazgovarao o tome kako moje vještine mogu doprinijeti vašim trenutnim projektima, u terminu koji vama najviše odgovara.

Unaprijed hvala na izdvojenom vremenu i razmatranju prijave.

S poštovanjem,
Nedim Jugo
+387 60 318 5869
nedim.jugoo@gmail.com");

        AddInteraction(appEndava, contBedirhan, InteractionDirection.Inbound, InteractionChannel.Email,
            new DateTime(2026, 9, 9, 9, 52, 0, DateTimeKind.Utc),
            "RE: Prijava za poziciju Junior Developer – Bedirhan Ramiz",
@"Pozdrav Nedime,

hvala ti na javljanju i interesovanju da se pridružiš nama u Endavi.
Informacije koje su pružio su jako korisne, ali nažalost, trenutak nije odgovarajući.
Trenutno nemamo prostora za neke nove kolege u našoj zemlji pa ću ti se zbog toga za ovaj put zahvaliti.
U slučaju da dođe do nekih promjena, biću slobodan da te kontaktiram.
Sretno u narednom periodu!

Lijepi pozdravi,
Bedirhan

Bedirhan Ramiz
Talent Acquisition Specialist
Mobile: +38761338932
Based in: Sarajevo");

        // ==========================================
        // 13. POPCORN RECRUITERS (Applied)
        // ==========================================
        var compPopcorn = await GetOrCreateCompany("Popcorn Recruiters", "https://popcornrecruiters.com", "Sarajevo, BiH", "Talent Acquisition Agency");
        var contSarah = CreateContact("Sarah Purić", compPopcorn.Id, "Recruiter", "sarah@popcornrecruiters.com", null, null, ContactType.Recruiter);
        var appPopcorn = CreateApplication(
            compPopcorn,
            "IT pozicija / Baza kandidata (Talentor Western Balkans)",
            ApplicationStatus.Applied,
            new DateTime(2026, 9, 29, 21, 0, 0, DateTimeKind.Utc),
            ApplicationSource.Referral,
            "sarah@popcornrecruiters.com",
            WorkMode.Remote,
            "Oni nisu firma javio se da me imaju u bazi dobio odgovor.",
            null);
        LinkApplicationContact(appPopcorn, contSarah);

        AddInteraction(appPopcorn, contSarah, InteractionDirection.Inbound, InteractionChannel.Email,
            new DateTime(2026, 9, 29, 21, 0, 0, DateTimeKind.Utc),
            "Potvrda prijave na otvorenu poziciju – Sarah Purić",
@"Pozdrav!

Hvala Vam na prijavi na otvorenu poziciju!

Vaša prijava je uspješno zaprimljena i trenutno je u procesu pregleda. Javit ćemo Vam se ukoliko Vaš profil odgovara zahtjevima pozicije i pozvati u sljedeću fazu selekcije.

Srdačan pozdrav,

Talentor Western Balkans");

        // ==========================================
        // 14. PORT8 D.O.O. MOSTAR (Rejected)
        // ==========================================
        var compPort8 = await GetOrCreateCompany("Port8 (emonitor AG)", "https://port8.ba", "Mostar, BiH", "PropTech / Software");
        var contPort8 = CreateContact("Port8 Careers", compPort8.Id, "HR / Recruiting Team", "careers@port8.ba", null, null, ContactType.Recruiter);
        var appPort8 = CreateApplication(
            compPort8,
            "Junior Software Developer",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 8, 15, 2, 0, DateTimeKind.Utc),
            ApplicationSource.CompanyWebsite,
            "careers@port8.ba online form",
            WorkMode.Onsite,
            "Javio se preko forme na port8.ba sa detaljnim cover letterom (Maker Faire, ESP32, GPS, Binny, EcoChallenge). Dobio odgovor.",
            "Trenutno nemamo otvorenu poziciju koja bi odgovarala Vašim kompetencijama.");
        LinkApplicationContact(appPort8, contPort8);

        AddInteraction(appPort8, contPort8, InteractionDirection.Outbound, InteractionChannel.Email,
            new DateTime(2026, 9, 8, 15, 2, 0, DateTimeKind.Utc),
            "port8.ba careers application from Nedim Jugo",
@"Poštovani Port8 tim,
Zovem se Nedim Jugo, bachelor softverskog inženjeringa sa Univerziteta „Džemal Bijedić"" u Mostaru, i javljam se putem otvorene, spontane aplikacije za junior developersku poziciju.

Port8 je mostarski PropTech startup, većinski u vlasništvu švicarske kompanije emonitor AG, koji digitalizuje tržište nekretnina. Na vašem sajtu sam vidio da izričito pozivate na spontane prijave i onda kad nemate otvorenu poziciju, pa se javljam za junior developersku poziciju u nadi da biste imali mjesta za nekoga ko želi rasti zajedno sa malim, brzorastućim timom.

Iza sebe imam tromjesečnu praksu u Garaža Makerspace-u, gdje sam radio na dvojezičnoj web stranici za Maker Faire Mostar, EU-finansirani festival s korporativnim sponzorima na nacionalnom nivou, i preuzeo tehničku vodeću ulogu i na tom projektu i na Smart Storage-u, IoT rješenju koje kombinuje web aplikaciju, ESP32 mikrokontroler i prepoznavanje glasa.
U posljednjih godinu dana sam samostalno izgradio i objavio nekoliko projekata u produkciji, svi javno dostupni kao open-source: GPS, full-stack platformu za javni gradski prijevoz (Angular, ASP.NET Core, Microsoft SQL Server); Binny, pametni sistem za sortiranje otpada (Raspberry Pi, YOLOv8, FastAPI); i EcoChallenge, ekološku Flutter aplikaciju s ASP.NET Core backendom i Stripe integracijom.

Ovo iskustvo mi je dalo praktično razumijevanje cijelog razvojnog ciklusa – dizajn baze podataka i API-ja, autentifikaciju, deployment na Azure, rad s Dockerom – ne samo teoriju sa fakulteta. Podjednako se snalazim u backendu (ASP.NET Core, Node.js, FastAPI) i frontendu (React, Angular, Flutter), što mi se čini korisno u manjem timu gdje svako pokriva više od jedne uloge.

U prilogu šaljem CV s detaljnim pregledom projekata i iskustva. Rado bih na razgovoru detaljnije predstavio svoj rad i motivaciju za pridruživanje vašem timu.

Unaprijed zahvaljujem na vremenu i razmatranju moje prijave.
S poštovanjem,
Nedim Jugo");

        AddInteraction(appPort8, contPort8, InteractionDirection.Inbound, InteractionChannel.Email,
            new DateTime(2026, 9, 8, 15, 43, 0, DateTimeKind.Utc),
            "Odg: port8.ba careers application from Nedim Jugo – Odbijenica",
@"Poštovana Nedim,

hvala Vam na prijavi i interesu za posao u Port8. Nažalost, trenutno nemamo otvorenu poziciju koja bi odgovarala Vašim kompetencijama.

Zadržat ćemo Vaš CV u našoj arhivi kako bismo Vas kontaktirali ako se otvori mjesto za Vas u budućnosti.

Još jednom se zahvaljujemo na prijavi i želimo Vam puno sreće i uspjeha u životu i radu.

Srdačan pozdrav!
Port8");

        // ==========================================
        // 15. NLB BANKA (Ghosted)
        // ==========================================
        var compNLB = await GetOrCreateCompany("NLB Banka", "https://www.nlb.ba", "Sarajevo / Mostar, BiH", "Banking");
        var appNLB = CreateApplication(
            compNLB,
            "Junior Software Developer / IT sektor",
            ApplicationStatus.Ghosted,
            new DateTime(2026, 9, 6, 10, 0, 0, DateTimeKind.Utc),
            ApplicationSource.CompanyWebsite,
            "Online forma na web stranici",
            WorkMode.Onsite,
            "NLB Banka prije 25 dana poslao podatke putem forme nista nisam dobio odgovor.",
            "Poslao podatke putem forme, nema odgovora.");

        AddInteraction(appNLB, null, InteractionDirection.Outbound, InteractionChannel.Other,
            new DateTime(2026, 9, 6, 10, 0, 0, DateTimeKind.Utc),
            "Online prijava za posao – NLB Banka",
            "NLB Banka prije 25 dana poslao podatke putem forme nista nisam dobio odgovor.");

        // ==========================================
        // 16. ASA BANKA (Ghosted)
        // ==========================================
        var compASA = await GetOrCreateCompany("ASA Banka", "https://www.asabanka.ba", "Sarajevo / Mostar, BiH", "Banking");
        var appASA = CreateApplication(
            compASA,
            "Junior Software Developer / IT sektor",
            ApplicationStatus.Ghosted,
            new DateTime(2026, 9, 6, 10, 30, 0, DateTimeKind.Utc),
            ApplicationSource.CompanyWebsite,
            "Online forma na web stranici",
            WorkMode.Onsite,
            "ASA Banka prije 25 dana poslao podatke putem forme nista nisam dobio odgovor.",
            "Poslao podatke putem forme, nema odgovora.");

        AddInteraction(appASA, null, InteractionDirection.Outbound, InteractionChannel.Other,
            new DateTime(2026, 9, 6, 10, 30, 0, DateTimeKind.Utc),
            "Online prijava za posao – ASA Banka",
            "ASA Banka prije 25 dana poslao podatke putem forme nista nisam dobio odgovor.");

        // ==========================================
        // 17. ZIRAAT BANK BH (Ghosted)
        // ==========================================
        var compZiraat = await GetOrCreateCompany("Ziraat Bank BH", "https://www.ziraatbank.ba", "Sarajevo / Mostar, BiH", "Banking");
        var appZiraat = CreateApplication(
            compZiraat,
            "Junior Software Developer / IT sektor",
            ApplicationStatus.Ghosted,
            new DateTime(2026, 9, 6, 11, 0, 0, DateTimeKind.Utc),
            ApplicationSource.CompanyWebsite,
            "Online forma na web stranici",
            WorkMode.Onsite,
            "Ziraat Banka prije 25 dana poslao podatke putem forme nista nisam dobio odgovor.",
            "Poslao podatke putem forme, nema odgovora.");

        AddInteraction(appZiraat, null, InteractionDirection.Outbound, InteractionChannel.Other,
            new DateTime(2026, 9, 6, 11, 0, 0, DateTimeKind.Utc),
            "Online prijava za posao – Ziraat Bank BH",
            "Zirat Banka prije 25 dana posalo podatke putem forme nista nisam dobio odgovor");

        // ==========================================
        // 18. PERSONIFY HEALTH (Salt Square / Mirza - Ghosted)
        // ==========================================
        var compPersonify = await GetOrCreateCompany("Personify Health (Virgin Pulse)", "https://personifyhealth.com", "Tuzla / Remote, BiH", "Digital Health");
        var contMirza = CreateContact("Mirza (Salt Square)", compPersonify.Id, "Engineering Lead / Director", "mirza@saltsquare.io", null, null, ContactType.Referrer, "Slao mirzi mejl i preporuku za Personify Health");
        var contDijana = CreateContact("Dijana", compPersonify.Id, "Referrer", null, null, null, ContactType.Referrer, "Preporuka za razgovor sa Mirzom");
        var appPersonify = CreateApplication(
            compPersonify,
            "Junior Developer / Junior Project Manager",
            ApplicationStatus.Ghosted,
            new DateTime(2026, 9, 13, 13, 58, 0, DateTimeKind.Utc),
            ApplicationSource.Referral,
            "mirza@saltsquare.io (preporuka Dijane)",
            WorkMode.Remote,
            "Slao Mirzi mejl i preporuku cv za personify health nema odgovora.",
            "Slao Mirzi mejl i preporuku cv za personify health nema odgovora.");
        LinkApplicationContact(appPersonify, contMirza);
        LinkApplicationContact(appPersonify, contDijana);

        AddInteraction(appPersonify, contMirza, InteractionDirection.Outbound, InteractionChannel.Email,
            new DateTime(2026, 9, 13, 13, 58, 0, DateTimeKind.Utc),
            "Prijava i CV povodom razgovora s Dijanom – Nedim Jugo",
@"Pozdrav Mirza,

Moje ime je Nedim Jugo i javljam se povodom razgovora koji si imao sa Dijanom u vezi mene i potencijalnim otvorenim pozicijama u Vašem timu.

Završio sam Fakultet informacijskih tehnologija (FIT) u Mostaru i stekao titulu bachelora softverskog inženjeringa (240 ECTS). Tokom studija i kroz samostalne projekte fokusirao sam se na full-stack razvoj (primarno C#, ASP.NET Core, Angular, SQL Server, kao i React / Flutter), uz rad na AI i embedded sistemima. Pored čistog inženjerskog dijela, kroz dugogodišnje vođenje i organizaciju timskih projekata, edukativnih aktivnosti i događaja stekao sam snažan osjećaj za planiranje, praćenje rokova i koordinaciju, zbog čega sam podjednako otvoren i motivisan i za Junior Developer i za Junior Project Manager poziciju, naravno sve ovisno o tome šta je trenutno dostupno otvoren sam i za prijedloge.

U prilogu ti šaljem svoj CV i motivaciono pismo, pripremljene na bosanskom i engleskom jeziku.

Stojim na raspolaganju za sastanak i razgovor u terminu kada Vama odgovara kako bi me tom prilikom upoznali i vidjeli kako bih se najbolje mogao uklopiti u Vaše trenutne projekte i potrebe tima.

Hvala ti na izdvojenom vremenu.

Srdačan pozdrav,

Nedim Jugo
0603185869
nedim.jugoo@gmail.com");

        // ==========================================
        // 19. INFOBIP (Ghosted)
        // ==========================================
        var compInfobip = await GetOrCreateCompany("Infobip", "https://www.infobip.com", "Sarajevo / Tuzla, BiH", "Cloud Communications Platform");
        var contInfobipTA = CreateContact("Infobip Talent Acquisition", compInfobip.Id, "Recruitment Team", "talent.acquisition@infobip.com", null, null, ContactType.Recruiter);
        var contKanita = CreateContact("Kanita", compInfobip.Id, "Infobip Kontakt", null, null, null, ContactType.Peer, "Kanita predložila slanje direktnog mejla");
        var appInfobip = CreateApplication(
            compInfobip,
            "Junior Software Developer / Junior Project Manager",
            ApplicationStatus.Ghosted,
            new DateTime(2026, 9, 15, 15, 55, 0, DateTimeKind.Utc),
            ApplicationSource.Referral,
            "talent.acquisition@infobip.com",
            WorkMode.Hybrid,
            "Danas sam samoinicijativno kontaktirao Vašu kolegicu Kanitu koja mi je predložila da Vam se javim direktno na ovaj mejl. Nema odgovora.",
            "Slao mejl na talent.acquisition@infobip.com, nema odgovora.");
        LinkApplicationContact(appInfobip, contInfobipTA);
        LinkApplicationContact(appInfobip, contKanita);

        AddInteraction(appInfobip, contInfobipTA, InteractionDirection.Outbound, InteractionChannel.Email,
            new DateTime(2026, 9, 15, 15, 55, 0, DateTimeKind.Utc),
            "Prijava za junior poziciju – Nedim Jugo (talent.acquisition@infobip.com)",
@"Poštovani,

Zovem se Nedim Jugo, bachelor softverskog inženjeringa iz Mostara, diplomirao sam na Univerzitetu „Džemal Bijedić“. Danas sam samoinicijativno kontaktirao Vaši kolegicu Kanitu, koja mi je odgovorila na neka pitanja ali i predložila da Vam se javim direktno na ovaj mejl i pošaljem svoj portfolio, pa se ovim putem javljam i raspitujem o mogućnosti prijave.

Da vam ukratko kažem nešto o sebi - iza sebe imam tromjesečnu praksu u Garaža Makerspace-u, gdje sam bio dio tima koji je razvio zvaničnu, dvojezičnu web stranicu za Maker Faire Mostar (EU-finansirani festival sa korporativnim sponzorima), a pored uloge developera sam preuzeo i vodeću tehničku ulogu na tom projektu, kao i na Smart Storage-u, IoT sistemu koji spaja web aplikaciju, ESP32 mikrokontroler i prepoznavanje glasa.

Osim toga, u posljednjih godinu dana sam samostalno izgradio i objavio nekoliko projekata u produkciji, svaki javno dostupan kao open-source i svaki u drugačijem domenu:

GPS – full-stack platforma za javni gradski prijevoz (Angular, ASP.NET Core, Microsoft SQL Server)
Binny – pametan sistem za sortiranje otpada (Raspberry Pi, YOLOv8, FastAPI)
EcoChallenge – ekološka Flutter aplikacija sa ASP.NET Core backendom i Stripe integracijom
Chord Hub – projekat vezan za muziku
Kroz ove projekte sam prošao kompletan razvojni ciklus – dizajn baze podataka i API-ja, autentifikaciju, deployment na Azure, rad s Dockerom – ne samo teoriju sa fakulteta. Podjednako se snalazim u backendu (ASP.NET Core, Node.js, FastAPI) i frontendu (React, Angular, Flutter), a kroz vođenje tehničke strane projekata sam se navikao i na planiranje zadataka, komunikaciju sa timom i praćenje rokova, što mi je jedan od razloga zašto mi je baš hibridna junor software dev ili junior project menadžer uloga privlačna – volim i pisati kod i organizovati rad oko projekta, ali naravno otvoren sam i za prijedloge ukoliko me vidite u nekom drugom timu.

U prilogu šaljem i motivaciono pismo i CV sa detaljnijim pregledom iskustva i projekata. Rado bih volo čuti povrate informacije i ima li trenutno otvorene prilike ovog tipa ili mogućnosti za kratak razgovor.

Unaprijed hvala izdvojenom vremenu,
Nedim Jugo
+387 60 318 5869
nedim.jugoo@gmail.com");

        // ==========================================
        // 20. LIDL BIH (Ghosted)
        // ==========================================
        var compLidl = await GetOrCreateCompany("Lidl BiH", "https://posao.lidl.ba", "Sarajevo / Mostar, BiH", "Retail & Internal IT");
        var contLidl = CreateContact("Lidl Posao", compLidl.Id, "HR / Recruiting Team", "posao@lidl.ba", null, null, ContactType.Recruiter);
        var appLidl = CreateApplication(
            compLidl,
            "Junior Developer – IT Odjel",
            ApplicationStatus.Ghosted,
            new DateTime(2026, 9, 15, 16, 27, 0, DateTimeKind.Utc),
            ApplicationSource.Other,
            "posao@lidl.ba",
            WorkMode.Hybrid,
            "Slao mejl nema odgovora. Prijava za poziciju u IT odjelu – Nedim Jugo.",
            "Slao mejl na posao@lidl.ba, nema odgovora.");
        LinkApplicationContact(appLidl, contLidl);

        AddInteraction(appLidl, contLidl, InteractionDirection.Outbound, InteractionChannel.Email,
            new DateTime(2026, 9, 15, 16, 27, 0, DateTimeKind.Utc),
            "Prijava za poziciju u IT odjelu – Nedim Jugo (posao@lidl.ba)",
@"Poštovani Lidl tim,

Zovem se Nedim Jugo, diplomirani inženjer softvera (Univerzitet „Džemal Bijedić“, Mostar). Javljam se u vezi mogućnosti zaposlenja u IT odjelu Lidl BiH, ukoliko trenutno postoji potreba za nekim na toj poziciji.

Svjestan sam da Lidl prije svega posluje u maloprodaji, ali mi je poznato da savremeni trgovački lanci danas imaju ozbiljne interne IT timove koji drže digitalizaciju poslovanja - od kasa i logistike do internih sistema - pa mi se čini da bi to mogla biti dobra prilika za mene kao junior developera.

U prilogu vam šaljem CV i motivaciono pismo sa detaljnijim pregledom dosadašnjeg iskustva i projekata. Rado bih razgovarao o tome da li trenutno postoji prostor za mene u vašem timu.

Unaprijed zahvaljujem na vremenu i odgovoru.

S poštovanjem,
Nedim Jugo
+387 60 318 5869
nedim.jugoo@gmail.com");

        // ==========================================
        // 21. MINISTRY OF PROGRAMMING (Rejected after phone screening)
        // ==========================================
        var compMoP = await GetOrCreateCompany("Ministry of Programming", "https://ministryofprogramming.com", "Sarajevo, BiH (Full Remote)", "Venture Builder & Software");
        var contResad = CreateContact("Rešad Začina", compMoP.Id, "Co-founder & CGO", "resad@ministryofprogramming.com", null, "https://www.linkedin.com/in/resadzacina", ContactType.HiringManager, "CGO Ministry of Programming");
        var contLejla = CreateContact("Lejla Imamović", compMoP.Id, "People Operations", "lejla.imamovic@ministryofprogramming.com", null, null, ContactType.Recruiter);
        var contHajra = CreateContact("Hajra Saletović", compMoP.Id, "People Operations / HR", "hajra.saletovic@ministryofprogramming.com", "+387 60 318 5869", null, ContactType.Recruiter, "Obavila telefonski screening poziv");
        var appMoP = CreateApplication(
            compMoP,
            "Junior Software Developer / Junior Project Manager",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 12, 12, 56, 0, DateTimeKind.Utc),
            ApplicationSource.LinkedIn,
            "LinkedIn direktni DM Rešadu Začini",
            WorkMode.Remote,
            "Javio se Rešadu na LinkedIn-u koji me uvezivao sa Lejlom i Hajrom na mailu. Poslao dvojezičnu prijavu. Hajra me zvala na telefon i pričali jedno 7min i rekla da sad nemaju ništa za mene.",
            "Hajra me zvala na telefon i pričali jedno 7 min i rekla da sad nemaju ništa za mene.");
        LinkApplicationContact(appMoP, contResad);
        LinkApplicationContact(appMoP, contLejla);
        LinkApplicationContact(appMoP, contHajra);

        AddInteraction(appMoP, contResad, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 12, 12, 56, 0, DateTimeKind.Utc),
            "Prijava u vezi moguće pozicije – Rešad Začina (12:56 PM)",
@"Poštovanje Rešade,

Vidio sam Vaš post gdje ste pozvali zainteresirane da se jave u DM, pa se javljam vezano za junior dev poziciju u Ministry of Programming. Znam da je subota i da vjerovatno nije idealno vrijeme za ovakvu poruku, ali sam odlučio da ne čekam i da Vam se javim odmah, kako ne bi propustio priliku.🙂

Živim u Mostaru, i s obzirom da ovdje trenutno nemate ured, htio bih pitati postoji li kod vas mogućnost remote ili hybrid angažmana za nekoga na junior poziciji.

Iza sebe imam nekoliko full-stack projekata koje sam sam vodio od ideje do produkcije, radim uglavnom sa ASP.NET Core, Angular, React i SQL Server bazama. Ali i pored svega ovoga imam završen Bachelor softverskog inženjeringa.

Bilo bi mi drago ako biste mogli pogledati moj profil i javiti mi ima li trenutno prostora za nekoga s mojim profilom. Ono što mogu garantovati je puno prilagođavanje Vašim standardima, ali i vidim tu priliku za dalje učenje i napredovanje. Otvoren sam za razgovor kad god Vama odgovara.

Hvala Vam puno i lijep pozdrav,
Nedim");

        AddInteraction(appMoP, contResad, InteractionDirection.Inbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 18, 12, 10, 0, DateTimeKind.Utc),
            "Odgovor Rešada Začine (12:10 PM)",
@"cao Nedime

svakako cu te uvezati sa nasim menadzmentom da te imaju u vidu

imamo uvijek prostora za dobre ljude

a svi su full remote tako da ne brini ;)

posalji mi samo full cv pls");

        AddInteraction(appMoP, contResad, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 18, 12, 24, 0, DateTimeKind.Utc),
            "Slanje CV-a i motivacionog pisma Rešadu Začini (12:24 PM)",
@"Poštovanje ponovo Rešade jako mi je drago što mi želite pomoći i što ste odgovorili na poruku a ja Vama šaljem dva dokumenta CV i motivaciono koji će nadam se biti dovoljni Vašim timovima da me razmotre za razgovor kako bi vidjeli moje vještine. 

Nedim_Jugo_CV_bosanski.docx
Motivaciono_pismo_MinistryOfProgramming.docx");

        AddInteraction(appMoP, contResad, InteractionDirection.Inbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 18, 12, 28, 0, DateTimeKind.Utc),
            "Odgovor Rešada Začine (12:28 PM)",
            "nema na cemu!");

        AddInteraction(appMoP, contResad, InteractionDirection.Inbound, InteractionChannel.Email,
            new DateTime(2026, 9, 18, 12, 46, 0, DateTimeKind.Utc),
            "Connecting with Nedim Jugo – Rešad Začina za Lejlu i Hajru",
@"Hi Lejla & Hajra,

Connecting you with Nedim Jugo. He indicated interest to work with us at MoP and hear more about the opportunities.

Thanks,
Resad");

        AddInteraction(appMoP, contHajra, InteractionDirection.Outbound, InteractionChannel.Email,
            new DateTime(2026, 9, 18, 14, 40, 0, DateTimeKind.Utc),
            "Dvojezični odgovor Lejli, Hajri i Rešadu – Nedim Jugo (2:40 PM)",
@"Bosanski

Poštovane Lejla i Hajra,

Rešade, hvala puno na uvezivanju!

Drago mi je što imamo priliku stupiti u kontakt. Kao što je Rešad spomenuo, izuzetno sam zainteresovan za mogućnosti angažmana u Ministry of Programmingu, posebno na poziciji Junior Software Developera ili Junior Projekt Menadžera.

Ukratko o meni: diplomirani sam inženjer softverskog inženjeringa, sa praktičnim iskustvom u izradi full-stack aplikacija, od ideje do produkcije, prvenstveno koristeći ASP.NET Core, React, Angular i SQL Server. Motivisan sam praktičnim rješavanjem problema, kontinuiranim učenjem i radom u okruženju sa visokim inženjerskim standardima.

U prilogu vam dostavljam svoj CV i motivaciono pismo na engleskom i bosanskom.

Bilo bi mi drago da se čujemo putem kratkog uvodnog razgovora u terminu koji vama najviše odgovara.

Lijep pozdrav,
Nedim Jugo
Email: nedim.jugoo@gmail.com
Tel: +387 60 318 5869

English

Hi Lejla and Hajra,

Rešad, thank you very much for the introduction!

It's a pleasure to connect with both of you. As Resad mentioned, I am very excited about the opportunity to explore junior software engineering or junior project management roles and learn more about potential opportunities at Ministry of Programming.

A brief introduction: I hold a Bachelor's degree in Software Engineering, with hands-on experience building full-stack applications from concept to production, primarily utilizing ASP.NET Core, React, Angular, and SQL Server. I am highly motivated by practical problem-solving, continuous learning, and working in an environment with high engineering standards.

Attached to this email, you will find my CV and Cover Letter in English and Bosnian.

I would welcome the opportunity for a brief introductory call whenever your schedule permits, so I can introduce myself in more detail.

Best regards,
Nedim Jugo
Email: nedim.jugoo@gmail.com
Tel: +387 60 318 5869");

        AddInteraction(appMoP, contResad, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 28, 13, 32, 0, DateTimeKind.Utc),
            "Follow-up upit Rešadu Začini (1:32 PM)",
@"Pozdrav, ja se ponovo javljam samo kako bi provjerio situaciju oko pozicija i mogućnost u vašim timovima, pošto ste mi se Vi jedini odazvali i odgovorili. Ja bi samo volio znati da li mogu dobiti bilo kakvu informaciju od Vaših kolegica ili od vas u vezi pozicija, posto od onog dana kada ste nas Vi povezali prije 10 dana nisam dobio nikakvu povratnu informaciju. Pa bi volio čisto da znam otprilike na čemu sam, ako to nije problem, ili ostaje da još moram čekati odgovor.");

        AddInteraction(appMoP, contResad, InteractionDirection.Inbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 28, 14, 39, 0, DateTimeKind.Utc),
            "Odgovor Rešada Začine (2:39 PM)",
@"hvala na javljanju

mislim da su imale dosta posla i kandidata

pingam ih svakako");

        AddInteraction(appMoP, contResad, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 28, 15, 20, 0, DateTimeKind.Utc),
            "Zahvala Rešadu Začini (3:20 PM)",
@"Hvala Vama na pomoći i stvarno brzim odgovorima. Vjerujem da kolegice imaju dosta posla u rukama, meni je bilo bitno samo da saznam da ću dobiti odgovor, a sad vjerujem da hoću u narednim danima");

        AddInteraction(appMoP, contHajra, InteractionDirection.Inbound, InteractionChannel.Phone,
            new DateTime(2026, 9, 30, 11, 0, 0, DateTimeKind.Utc),
            "Telefonski screening poziv sa Hajrom Saletović (7 min)",
            "onda me hajra jucer zvala na telefon i pricali jedno 7min i rekla da sad nemaju nista za mene");

        // Add Interview for MoP
        var interviewMoP = new Interview
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ApplicationId = appMoP.Id,
            Type = InterviewType.HR,
            Format = InterviewFormat.Phone,
            Status = InterviewStatus.Completed,
            ScheduledAt = new DateTime(2026, 9, 30, 11, 0, 0, DateTimeKind.Utc),
            DurationMinutes = 7,
            PrepNotes = "Telefonski screening poziv sa Hajrom Saletović.",
            OutcomeNotes = "Telefonski screening poziv (7 min). Zaključak: trenutno nemaju otvorenih pozicija za junior developere.",
            CreatedAt = new DateTime(2026, 9, 30, 11, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 9, 30, 11, 10, 0, DateTimeKind.Utc)
        };
        db.Interviews.Add(interviewMoP);
        db.InterviewContacts.Add(new InterviewContact
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            InterviewId = interviewMoP.Id,
            ContactId = contHajra.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // ==========================================
        // 22. XSOFT (Rejected)
        // ==========================================
        var compXSoft = await GetOrCreateCompany("XSoft Mostar", "https://x-soft.ba", "Mostar, BiH", "Software Development");
        var contMaja = CreateContact("Maja (XSoft)", compXSoft.Id, "HR / Liaison", null, null, null, ContactType.Referrer, "Proslijedila CV Danijelu");
        var contDanijel = CreateContact("Danijel (XSoft)", compXSoft.Id, "Dev Lead / Manager", null, null, null, ContactType.HiringManager, "Odgovorio da nema nista sad");
        var appXSoft = CreateApplication(
            compXSoft,
            "Junior Software Developer",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 19, 10, 0, 0, DateTimeKind.Utc),
            ApplicationSource.Referral,
            "Preporuka preko Maje, proslijeđeno Danijelu",
            WorkMode.Onsite,
            "XSoft slao Maji CV i motivaciono prije 12 dana ona poslala Danijelu on prije 4 dana odvoroio da nema nista sad za mene.",
            "Danijel odgovorio da nema nista sad za mene.");
        LinkApplicationContact(appXSoft, contMaja);
        LinkApplicationContact(appXSoft, contDanijel);

        AddInteraction(appXSoft, contMaja, InteractionDirection.Outbound, InteractionChannel.Email,
            new DateTime(2026, 9, 19, 10, 0, 0, DateTimeKind.Utc),
            "Slanje CV-a i motivacionog pisma – Maja (XSoft)",
            "Slao Maji CV i motivaciono pismo za junior dev poziciju u XSoft-u kako bi proslijedila timu na razmatranje.");

        AddInteraction(appXSoft, contDanijel, InteractionDirection.Inbound, InteractionChannel.Email,
            new DateTime(2026, 9, 27, 14, 0, 0, DateTimeKind.Utc),
            "Povratna informacija – Danijel (XSoft)",
            "Maja poslala Danijelu, on odgovorio da nema nista sad za mene.");

        // ==========================================
        // 23. CONFIGPOS (Rejected)
        // ==========================================
        var compConfig = await GetOrCreateCompany("Config d.o.o. Mostar", "https://config.ba", "Mostar, BiH", "POS Systems & Software");
        var contConfig = CreateContact("Config Office", compConfig.Id, "Management / HR", null, null, null, ContactType.Recruiter);
        var appConfig = CreateApplication(
            compConfig,
            "Junior Software Developer",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 27, 10, 30, 0, DateTimeKind.Utc),
            ApplicationSource.Other,
            "Telefonski poziv",
            WorkMode.Onsite,
            "ConfigPOS zvao prije 4 dana nema niakakvih pzoicija.",
            "Zvao telefonom, nema nikakvih otvorenih pozicija.");
        LinkApplicationContact(appConfig, contConfig);

        AddInteraction(appConfig, contConfig, InteractionDirection.Outbound, InteractionChannel.Phone,
            new DateTime(2026, 9, 27, 10, 30, 0, DateTimeKind.Utc),
            "Telefonski upit za otvorene pozicije – Config d.o.o.",
            "ConfigPOS zvao prije 4 dana nema niakakvih pzoicija");

        // ==========================================
        // 24. BLOOMTEQ (Rejected)
        // ==========================================
        var compBloomteq = await GetOrCreateCompany("Bloomteq", "https://bloomteq.com", "Sarajevo / Mostar, BiH", "Software Engineering");
        var contBabovic = CreateContact("Elmir Babović", compBloomteq.Id, "Tech Lead / Management", null, null, null, ContactType.HiringManager);
        var appBloomteq = CreateApplication(
            compBloomteq,
            "Junior Software Developer",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 20, 15, 0, 0, DateTimeKind.Utc),
            ApplicationSource.LinkedIn,
            "LinkedIn direktna poruka",
            WorkMode.Remote,
            "Bloomteq pisao Elmiru Babovicu i nista nema sad.",
            "Pisao Elmiru Baboviću i ništa nema sad (odgovorio samo sa 👍).");
        LinkApplicationContact(appBloomteq, contBabovic);

        AddInteraction(appBloomteq, contBabovic, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 20, 15, 0, 0, DateTimeKind.Utc),
            "Poruka Elmiru Baboviću – Nedim Jugo",
@"Nisam Vas direktno tražio, ali kada sam Vas ipak vidio, nisam Vas htio puno ometati jer ste bili na ispitu. Htio bi Vam reci da ako bio bih Vam veoma zahvalan ako biste me imali na umu ili eventualno preporučili ukoliko se pojavi neka prilika u IT-ju.

Kao što znate, ja sam iz Mostara, pa bi mi zbog toga najviše odgovarala neka firma u Mostaru ili ako je opcija van Mostara da omogućava hybrid ili remote rad. Naravno, otvoren sam i za druge opcije, ali bi mi trenutno preseljenje u Sarajevo zbog troškova života bilo dosta teško izvodljivo.

Poslat ću Vam ovdje i svoj CV, pa ga možete pogledati kada budete imali vremena, čisto da imate bolji uvid u moje dosadašnje iskustvo, projekte i rad.

Hvala Vam unaprijed na vremenu i pomoći, zaista bih bio zahvalan na komunikaciji i prijašnjem savijetu, a ja Vas neću više ovdje zamarati i pisati.");

        AddInteraction(appBloomteq, contBabovic, InteractionDirection.Inbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 20, 16, 30, 0, DateTimeKind.Utc),
            "Odgovor Elmira Babovića",
            "👍");

        // ==========================================
        // 25. EVONA (Rejected)
        // ==========================================
        var compEVONA = await GetOrCreateCompany("EVONA Electronic", "https://evona-electronic.com", "Mostar, BiH", "Software & Gaming Systems");
        var contMarijaEvona = CreateContact("Marija (EVONA)", compEVONA.Id, "EVONA Kontakt", null, null, null, ContactType.Peer);
        var contIvanEvona = CreateContact("Ivan (EVONA)", compEVONA.Id, "EVONA Kontakt", null, null, null, ContactType.Peer);
        var appEVONA = CreateApplication(
            compEVONA,
            "Junior Software Developer",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 1, 11, 0, 0, DateTimeKind.Utc),
            ApplicationSource.Referral,
            "Direktni upit Mariji i Ivanu",
            WorkMode.Onsite,
            "Pisao Mariji i Ivanu prije 30 dana i oboje rekli da nema pozicija.",
            "Pisao Mariji i Ivanu prije 30 dana i oboje rekli da nema pozicija.");
        LinkApplicationContact(appEVONA, contMarijaEvona);
        LinkApplicationContact(appEVONA, contIvanEvona);

        AddInteraction(appEVONA, contMarijaEvona, InteractionDirection.Outbound, InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 1, 11, 0, 0, DateTimeKind.Utc),
            "Upit za posao – Marija i Ivan (EVONA)",
            "Pisao Mariji i Ivanu prije 30 dana i oboje rekli da nema pozicija");

        // ==========================================
        // 26. ENTERWELL (Applied)
        // ==========================================
        var compEnterwell = await GetOrCreateCompany("Enterwell", "https://enterwell.net", "Mostar, BiH", "Software Development");
        var contEnterwell = CreateContact("Enterwell Careers Team", compEnterwell.Id, "Careers / Talent Acquisition", "careers@enterwell.net", null, null, ContactType.Recruiter);
        var appEnterwell = CreateApplication(
            compEnterwell,
            ".NET Developer",
            ApplicationStatus.Applied,
            new DateTime(2026, 10, 6, 15, 31, 0, DateTimeKind.Utc),
            ApplicationSource.Other,
            "Email prijava (careers@enterwell.net)",
            WorkMode.Onsite,
            "Prijava za .NET Developer poziciju poslata putem emaila na careers@enterwell.net. Priloženi CV i motivaciono pismo.",
            null);
        LinkApplicationContact(appEnterwell, contEnterwell);

        AddInteraction(appEnterwell, contEnterwell, InteractionDirection.Outbound, InteractionChannel.Email,
            new DateTime(2026, 10, 6, 15, 31, 0, DateTimeKind.Utc),
            "NET developer at Enterwell",
@"Nedim <nedim.jugoo@gmail.com>
to: careers@enterwell.net
date: Oct 6, 2026, 3:31 PM
subject: NET developer at Enterwell
mailed-by: gmail.com

Nedim <nedim.jugoo@gmail.com>
3:31 PM (10 minutes ago)
to careers

Dear Enterwell Team,
I am applying for the .NET Developer position. My motivation letter and CV are attached.

Contact information
Nedim Jugo
Mostar, Bosnia and Herzegovina
0603185869
nedim.jugoo@gmail.com

About me
I hold a bachelor's degree in Software Engineering from ""Džemal Bijedić"" University in Mostar. During my studies I focused on building complete, real-world applications rather than only academic exercises, and over the past year I have delivered several large projects.
My main technology is ASP.NET Core, which I use for backend development and APIs. I have used it in a Smart City application (Angular, ASP.NET Core) and in EcoChallenge (Flutter, ASP.NET Core), where I also integrated online payments with Stripe. I have worked with relational databases in all of my projects, and I have deployed applications on Microsoft Azure using Docker containers. On the AI side, I built Binny, an autonomous waste recognition and sorting system (YOLOv8, FastAPI), and Glyco, an AI-based system for tracking and diagnosing Type 2 diabetes. Both projects won awards.
I also completed a 3-month development internship at Garaža Makerspace in Mostar, where I built the website for Maker Faire Mostar (an EU-supported festival) and an IoT project, Smart Storage, connecting a web application to an ESP32 microcontroller. I have worked with clients and led teams on several projects, and I pick up new languages and frameworks quickly. Also if you see me fit for some other role I'm open for talk and consideration.

Why Enterwell
I want to work on high-quality software products in a team that values strong engineering standards and code quality, and I believe I can grow quickly in the .NET ecosystem with your team. And also you are in my town which wouldn't require me to move to another city.

Links
GitHub: https://github.com/NedimJugo
LinkedIn: https://www.linkedin.com/in/nedim-jugo-492b99277/
Portfolio: https://nedim-jugo.vercel.app/

Thank you for your time and consideration. I look forward to hearing from you.
Best regards,
Nedim Jugo
2 Attachments • Scanned by Gmail");

        // ==========================================
        // 27. IMPEREA (Applied)
        // ==========================================
        var compImperea = await GetOrCreateCompany("Imperea", "https://imperea.ba", "Mostar / Sarajevo, BiH", "Software Development & IT Consulting");
        var contImperea = CreateContact("Imperea Hiring Team", compImperea.Id, "Talent Acquisition / Management", "office@imperea.ba", null, null, ContactType.Recruiter);
        var appImperea = CreateApplication(
            compImperea,
            "Full Stack / .NET Core Developer",
            ApplicationStatus.Applied,
            new DateTime(2026, 10, 6, 15, 39, 0, DateTimeKind.Utc),
            ApplicationSource.Other,
            "Email prijava (office@imperea.ba)",
            WorkMode.Hybrid,
            "Prijava za Full Stack / .NET Core Developer poziciju putem emaila na office@imperea.ba. CV i motivaciono pismo priloženi na bosanskom i engleskom jeziku.",
            null);
        LinkApplicationContact(appImperea, contImperea);

        AddInteraction(appImperea, contImperea, InteractionDirection.Outbound, InteractionChannel.Email,
            new DateTime(2026, 10, 6, 15, 39, 0, DateTimeKind.Utc),
            "Prijava/Application: Full Stack / .NET Core Developer, Nedim Jugo",
@"Nedim <nedim.jugoo@gmail.com>
to: office@imperea.ba
date: Oct 6, 2026, 3:39 PM
subject: Prijava/Application: Full Stack / .NET Core Developer, Nedim Jugo
mailed-by: gmail.com

Nedim <nedim.jugoo@gmail.com>
3:39 PM (3 minutes ago)
to office

Poštovani Imperea timu,
Prijavljujem se na poziciju Full Stack / .NET Core Developera. Motivaciono pismo i CV nalaze se u prilogu ovog mejla, na bosanskom i engleskom jeziku.

O meni
Diplomirao sam softverski inženjering na Univerzitetu „Džemal Bijedić"" u Mostaru. Tokom studija fokusirao sam se na izradu cjelovitih, stvarnih aplikacija, a tokom protekle godine realizovao sam nekoliko većih projekata.
Najviše radim s ASP.NET Core platformom, Angularom i Flutterom. Kreirao sam aplikaciju za „pametni grad"" (Smart City) te aplikaciju EcoChallenge, u koju sam integrisao online plaćanje putem platforme Stripe. U svim projektima radio sam s relacijskim bazama podataka, a aplikacije sam postavljao na Microsoft Azure uz Docker kontejnere. Pored toga, izradio sam Binny, sistem za prepoznavanje i sortiranje otpada (YOLOv8, FastAPI), i Glyco, sistem zasnovan na vještačkoj inteligenciji za praćenje i dijagnostiku dijabetesa tipa 2. Oba projekta osvojila su nagrade.
Obavio sam i tromjesečnu praksu u „Garaža Makerspaceu"" u Mostaru, gdje sam izradio web stranicu za Maker Faire Mostar i IoT projekat Smart Storage. Sarađivao sam s klijentima, vodio timove na više projekata i brzo usvajam nove tehnologije.

Više detalja o mom iskustvu i projektima možete pronaći u priloženom CV-u i motivacionom pismu, kao i na sljedećim linkovima:
GitHub: https://github.com/NedimJugo
LinkedIn: https://www.linkedin.com/in/nedim-jugo-492b99277/
Portfolio: https://nedim-jugo.vercel.app/

Otvoren sam za razgovor o svim mogućnostima saradnje i bilo bi mi drago da se upoznamo.
Hvala vam na vremenu i razmatranju moje prijave. Radujem se vašem odgovoru.
Srdačan pozdrav,
Nedim Jugo
0603185869
nedim.jugoo@gmail.com
__________________________________________________________________________________________________
Dear Imperea Team,
I am applying for the Full Stack / .NET Core Developer position. My motivation letter and CV are attached to this email, in both Bosnian and English.

About me
I hold a bachelor's degree in Software Engineering from ""Džemal Bijedić"" University in Mostar. During my studies I focused on building complete, real-world applications, and over the past year I have delivered several larger projects.
I work mostly with ASP.NET Core, Angular and Flutter. I built a Smart City application and EcoChallenge, where I integrated online payments with Stripe. I have worked with relational databases in all of my projects, and I have deployed applications on Microsoft Azure using Docker containers. I also built Binny, a waste recognition and sorting system (YOLOv8, FastAPI), and Glyco, an AI-based system for tracking and diagnosing Type 2 diabetes. Both projects won awards.
I also completed a 3-month internship at Garaža Makerspace in Mostar, where I built the website for Maker Faire Mostar and an IoT project, Smart Storage. I have worked with clients, led teams on several projects, and I pick up new technologies quickly.

You can find more details about my experience and projects in the attached CV and motivation letter, as well as at the following links:
GitHub: https://github.com/NedimJugo
LinkedIn: https://www.linkedin.com/in/nedim-jugo-492b99277/
Portfolio: https://nedim-jugo.vercel.app/

I am open to discussing any opportunities for collaboration and would be happy to meet and talk.
Thank you for your time and consideration. I look forward to hearing from you.
Best regards,
Nedim Jugo
0603185869
nedim.jugoo@gmail.com");

        // ==========================================
        // 28. BS TELECOM SOLUTIONS (Applied)
        // ==========================================
        var compBSTelecom = await GetOrCreateCompany("BS Telecom Solutions", "https://bstsolutions.ba", "Branilaca Sarajeva 20, 71000 Sarajevo", "Telecommunications & Software Solutions");
        var contBSTelecom = CreateContact("BS Telecom HR / Recruitment", compBSTelecom.Id, "Recruitment Team (Asseco SEE)", null, null, null, ContactType.Recruiter, "Asseco South Eastern Europe S.A. / BS TS d.o.o. Sarajevo, Branilaca Sarajeva 20");
        var appBSTelecom = CreateApplication(
            compBSTelecom,
            "Software Engineer (Data & Analytics)",
            ApplicationStatus.Applied,
            new DateTime(2026, 10, 6, 15, 40, 0, DateTimeKind.Utc),
            ApplicationSource.CompanyWebsite,
            "Web karijerna forma (Asseco SEE / BS Telecom Solutions)",
            WorkMode.Hybrid,
            "Prijava putem web forme na stranici za poziciju Software Engineer (Data & Analytics). Priložen Nedim_Jugo_CV_english.pdf i popunjeno motivaciono pismo. Željena bruto plata: 1.750 – 2.000 KM.",
            null);
        LinkApplicationContact(appBSTelecom, contBSTelecom);

        AddInteraction(appBSTelecom, contBSTelecom, InteractionDirection.Outbound, InteractionChannel.Other,
            new DateTime(2026, 10, 6, 15, 40, 0, DateTimeKind.Utc),
            "Prijava preko web forme – Software Engineer (Data & Analytics)",
@"BS TELECOM SOLUTIONS (Asseco South Eastern Europe)
Position: Software Engineer (Data & Analytics) - Full-time
Location: BS Telecom Sarajevo, Branilaca Sarajeva 20; 71000 Sarajevo

Candidate: Nedim Jugo
Phone: +387 60 318 5869
Email: nedim.jugoo@gmail.com
Location: Mostar, BiH
Resume: Nedim_Jugo_CV_english.pdf
Desired salary (gross): 1.750 – 2000 KM

Cover letter:
Dear BS TS Team,

My name is Nedim Jugo, and I’m from Mostar. I earned my Software Engineering bachelor’s degree from “Džemal Bijedić” University. I am applying for the Software Engineer (Data & Analytics) position because I am interested in working with data and building systems that extract value from it, and BS Telekom offers me the chance to do that with large, real-world datasets.

I learned about BS Telekom while researching companies that invest in digital transformation and data work. My award-winning projects Binny (YOLOv8 + FastAPI) and Glyco (an AI-powered system for tracking and diagnosing Type 2 diabetes) show my experience working with data and models and integrating them into functional systems.
I worked a 3-month development internship for Garaža Makerspace in Mostar where I designed and built the website for the Maker Faire Mostar (an EU festival). As a web developer, I built and created an Internet of Things (IoT) project called Smart Storage. For this project, I interfaced and built a Web application with an ESP32 microcontroller, and integrated voice activation and recognition. I worked and interacted with many clients and colleagues, and was a team leader for many of the projects.

During the past year, I’ve created many large-scale projects, including a smart waste sorting and recycling system (Binny) with AI (YOLOv8) and FastAPI, and Glyco, a Type 2 diabetes diagnostics system. On the backend I worked with ASP.NET Core, and I also created a Smart City application with Angular, ASP.NET Core and services built with RTGS, and EcoChallenge (Flutter, ASP.NET Core) with an integrated Stripe online payment system.

Through my many projects, I created many secure Web applications and services. I learned how to deploy Web applications on Microsoft Azure. I learned Docker and other containerization techniques. I am able to easily adopt new programming languages and frameworks. I worked with relational databases in all of my projects, including schema design, writing queries, and data processing. During my studies, I had the opportunity to work with Power BI, where I loaded, cleaned, sorted, and filtered data and visualized it through reports. For my AI projects (Binny and Glyco), I independently prepared data for model training, including collecting, cleaning, and processing data, as well as monitoring and analyzing model results.

As a graduate student, I worked with several non-governmental and community organizations. I developed my skills in negotiation and communication through the design and implementation of several projects. My work helped me to become more flexible and understanding of various ways of thinking. I helped develop the skills of several people, including through the training and empowerment of dozens of people. I gained valuable expertise in several vocational skills, which enabled me to develop tangible products, increase my level of self-confidence, and encouraged me to plan for the future. My goal is to apply my software development and data skills to challenges in the telecommunications sector and keep growing with your team.

Thank you for your time and consideration.
Best regards,
Nedim Jugo

Links:
linkedin.com/in/nedim-jugo-492b99277
github.com/NedimJugo
nedim-jugo.vercel.app

Consents:
- Asseco South Eastern Europe S.A. recruitment privacy notice & data processing agreed.
- Future recruitment process consent agreed.");

        // ==========================================
        // Key Follow-up Tasks for Nedim
        // ==========================================
        db.Tasks.Add(new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ApplicationId = appEnterwell.Id,
            Title = "Follow up na prijavu za .NET Developer (Enterwell)",
            Notes = "Poslata prijava na careers@enterwell.net. Provjeriti status prijave ako nema odgovora za 7-10 dana.",
            DueAt = DateTime.UtcNow.AddDays(7),
            Source = TaskSource.Manual,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.Tasks.Add(new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ApplicationId = appImperea.Id,
            Title = "Follow up na prijavu za Full Stack / .NET Developer (Imperea)",
            Notes = "Poslata prijava na office@imperea.ba. Provjeriti status prijave ako nema odgovora za 7-10 dana.",
            DueAt = DateTime.UtcNow.AddDays(7),
            Source = TaskSource.Manual,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.Tasks.Add(new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ApplicationId = appBSTelecom.Id,
            Title = "Pratiti status prijave za Software Engineer (Data & Analytics) u BS Telecom",
            Notes = "Prijava poslata putem web forme (Asseco SEE / BS Telecom Solutions). Provjeriti status za 7-10 dana.",
            DueAt = DateTime.UtcNow.AddDays(7),
            Source = TaskSource.Manual,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.Tasks.Add(new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ApplicationId = appZIRA.Id,
            Title = "Pratiti objavu i prijaviti se na Dev ZTA internship (ZIRA)",
            Notes = "Armin Babović je preporučio da se prijavim na Dev ZTA (3 mjeseca prakse koja vodi ka stalnom zaposlenju) i javim Medini.",
            DueAt = DateTime.UtcNow.AddDays(7),
            Source = TaskSource.Manual,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.Tasks.Add(new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ApplicationId = appHTEC.Id,
            Title = "Pratiti HTEC kanale i pingati Damira Avdića",
            Notes = "Damir Avdić ponudio da provjeri sa TA timom ako se otvori juniorska uloga.",
            DueAt = DateTime.UtcNow.AddDays(14),
            Source = TaskSource.Manual,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.Tasks.Add(new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ApplicationId = appBHTelecom.Id,
            Title = "Pratiti objavu konkursa na bhtelecom.ba/karijera",
            Notes = "Prijem ide putem javnih oglasa uz mogućnost angažmana na određeno do 6 mjeseci.",
            DueAt = DateTime.UtcNow.AddDays(10),
            Source = TaskSource.Manual,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.Tasks.Add(new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ApplicationId = appGaleyo.Id,
            Title = "Periodično provjeravati Galeyo otvorene pozicije za Mostar",
            Notes = "Nina H. potvrdila da je profil sačuvan i da će kontaktirati ukoliko se otvori rola u mostarskom uredu.",
            DueAt = DateTime.UtcNow.AddDays(21),
            Source = TaskSource.Manual,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);

        return user;
    }
}
