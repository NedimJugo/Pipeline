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
                // Fallback direct add if UserManager has password policy constraints
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
            await db.SaveChangesAsync(ct);
        }

        var userId = user.Id;

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

        // Helper to find or create contact
        async Task<Contact> GetOrCreateContact(
            string fullName,
            Guid? companyId,
            string? role = null,
            string? email = null,
            string? phone = null,
            string? linkedIn = null,
            ContactType type = ContactType.Recruiter,
            string? notes = null)
        {
            var contact = await db.Contacts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.UserId == userId && c.FullName.ToLower() == fullName.ToLower(), ct);

            if (contact == null)
            {
                contact = new Contact
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
                await db.SaveChangesAsync(ct);
            }
            return contact;
        }

        // Helper to find or create application
        async Task<JobApplication> GetOrCreateApplication(
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
            var app = await db.Applications
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.UserId == userId && a.CompanyId == company.Id, ct);

            if (app == null)
            {
                app = new JobApplication
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
                    Location = company.Location ?? "Mostar / BiH",
                    Notes = notes,
                    ClosedReason = closedReason,
                    OfferSalary = offerSalary,
                    Currency = "BAM",
                    OfferNegotiationNotes = offerNotes,
                    CreatedAt = appliedAt,
                    UpdatedAt = DateTime.UtcNow
                };
                db.Applications.Add(app);
                await db.SaveChangesAsync(ct);

                // Add status history
                db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ApplicationId = app.Id,
                    FromStatus = ApplicationStatus.Wishlist,
                    ToStatus = ApplicationStatus.Applied,
                    ChangedAt = appliedAt,
                    Note = "Prijava uspješno evidentirana u sistemu."
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

                await db.SaveChangesAsync(ct);
            }
            else
            {
                // Ensure latest notes and status
                app.Status = status;
                app.AppliedAt = appliedAt;
                app.Notes = notes;
                app.ClosedReason = closedReason;
                if (offerSalary.HasValue)
                {
                    app.OfferSalary = offerSalary;
                    app.OfferNegotiationNotes = offerNotes;
                }
                await db.SaveChangesAsync(ct);
            }

            return app;
        }

        // Helper to add interaction if not already existing
        async Task AddInteraction(
            JobApplication? app,
            Contact? contact,
            InteractionDirection direction,
            InteractionChannel channel,
            DateTime occurredAt,
            string summary,
            string sentContent)
        {
            var exists = await db.Interactions
                .IgnoreQueryFilters()
                .AnyAsync(i => i.UserId == userId &&
                               i.ApplicationId == (app != null ? app.Id : (Guid?)null) &&
                               i.Summary == summary, ct);

            if (!exists)
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
        }

        // Helper to link contact to application
        async Task LinkApplicationContact(JobApplication app, Contact contact)
        {
            var exists = await db.ApplicationContacts
                .AnyAsync(ac => ac.ApplicationId == app.Id && ac.ContactId == contact.Id, ct);
            if (!exists)
            {
                db.ApplicationContacts.Add(new ApplicationContact
                {
                    ApplicationId = app.Id,
                    ContactId = contact.Id
                });
            }
        }

        // ==========================================
        // 1. RAIFFEISEN GROUP (f01)
        // ==========================================
        var cRaiffeisen = await GetOrCreateCompany("Raiffeisen Group (Raiffeisen banka)", "https://bosnia.recruiter.hr", "BiH", "Banking & Finance");
        var appRaiffeisen = await GetOrCreateApplication(
            cRaiffeisen,
            "Software Developer (otvorena prijava / konkurs)",
            ApplicationStatus.Ghosted,
            new DateTime(2026, 9, 1, 13, 49, 0, DateTimeKind.Utc),
            ApplicationSource.CompanyWebsite,
            "web portal (bosnia.recruiter.hr)",
            WorkMode.Hybrid,
            "Aplikacija preko stranice. Dobio mejl s pristupnim podacima za portal. Drugi odgovor nikad nisam dobio.\nIzvor: pokusaji.txt:1-28",
            "Samo automatska potvrda, bez odgovora više od 30 dana."
        );
        await AddInteraction(
            appRaiffeisen,
            null,
            InteractionDirection.Inbound,
            InteractionChannel.Email,
            new DateTime(2026, 9, 1, 13, 49, 0, DateTimeKind.Utc),
            "Obavijest o prijavi na konkurs (Raiffeisen Group)",
            "Poštovani/a,\nObavijest o prijavi na konkurs\n\nPoslodavac Raiffeisen Group Vam se zahvaljuje na prijavi te Vas obavještava da na adresi https://bosnia.recruiter.hr možete dodatno urediti svoj profil ili ostaviti informacije koje mogu biti korisne za nastavak selekcijskog postupka, a to možete učiniti sa sljedećim pristupnim podacima:\n\nKorisničko ime : ri9tg1h3\nLozinka : [IZOSTAVLJENO]\n\nPoslodavac Raiffeisen Group Vam se zahvaljuje što ste odvojili vrijeme i prijavili se na konkurs. U svom korisničkom profilu možete izmijeniti lozinku, dodati svoju sliku te ostaviti ili povući pristanak za obradu ličnih podataka.\n\nRaiffeisen Group i Recruiter"
        );

        // ==========================================
        // 2. UNICREDIT BANK BIH (f02)
        // ==========================================
        var cUniCredit = await GetOrCreateCompany("UniCredit Bank (BiH)", "https://www.unicreditgroup.ba", "Mostar / Sarajevo, BiH", "Banking & Finance");
        var appUniCredit = await GetOrCreateApplication(
            cUniCredit,
            "IT sektor (otvorena prijava)",
            ApplicationStatus.Withdrawn,
            new DateTime(2026, 8, 25, 10, 0, 0, DateTimeKind.Utc),
            ApplicationSource.Other,
            "email: prijavazaposao@unicreditgroup.ba",
            WorkMode.Hybrid,
            "Zvali me preko telefona ponudili u call centru 1450KM da radim ja odbio. Aplikacija za IT nije napredovala.\nIzvor: pokusaji.txt:56",
            "Odbio ponudu za call centar (nije ciljana uloga softverskog inženjera).",
            1450,
            "Ponuđena pozicija u Call Centru za 1450 KM mjesečno. Kandidat odbio jer traži poziciju u softverskom inženjeringu / IT sektoru."
        );
        await AddInteraction(
            appUniCredit,
            null,
            InteractionDirection.Outbound,
            InteractionChannel.Email,
            new DateTime(2026, 8, 25, 10, 0, 0, DateTimeKind.Utc),
            "Prijava za IT sektor UniCredit banke",
            "Poštovani UniCredit timu,\n\nJavljam Vam se kako bih iskazao interes za rad u IT sektoru UniCredit banke. Kao diplomant softverskog inženjeringa, izuzetno cijenim Vaš pristup tehnološkom razvoju i inovacijama u bankarstvu.\n\nMotivisan sam da svoju karijeru gradim u okruženju koje pruža priliku za kontinuirano učenje od vrhunskih stručnjaka, ali u kojem istovremeno mogu aktivno doprinijeti radu i uspjehu tima svojim dosadašnjim znanjem, radnim navikama i tehničkim vještinama.\n\nU prilogu Vam dostavljam svoj CV i motivaciono pismo, sa željom da pregledate moje kvalifikacije i projekte. Nadam se da ćete razmotriti moju prijavu za trenutne ili nadolazeće prilike u kojima bi moj profil mogao biti od koristi.\n\nBilo bi mi zadovoljstvo da Vam se i lično predstavim na razgovoru.\n\nUnaprijed se zahvaljujem na izdvojenom vremenu i pažnji.\n\nUgodan dan,\n\nNedim Jugo\n+38760 318 5869\nLinkedIn GitHub"
        );
        await AddInteraction(
            appUniCredit,
            null,
            InteractionDirection.Inbound,
            InteractionChannel.Phone,
            new DateTime(2026, 8, 30, 14, 0, 0, DateTimeKind.Utc),
            "Telefonski poziv: ponuda za Call centar",
            "Zvali me preko telefona ponudili u call centru 1450KM da radim ja odbio."
        );

        // ==========================================
        // 3. ITO MOSTAR (f03)
        // ==========================================
        var cIto = await GetOrCreateCompany("ITO (Mostar)", "https://ito.dev", "Mostar, BiH", "Software Development");
        var ctSanjaZovko = await GetOrCreateContact("Sanja Zovko", cIto.Id, "HR Manager", "sanja@ito.ba", "+387 (0) 63 685 886 / +387 (0) 36 830 003", null, ContactType.HiringManager);
        var appIto = await GetOrCreateApplication(
            cIto,
            "Junior developer (otvorena prijava)",
            ApplicationStatus.Rejected,
            new DateTime(2026, 8, 28, 11, 0, 0, DateTimeKind.Utc),
            ApplicationSource.Referral,
            "email: sanja@ito.ba (Preporuka: MarijaB)",
            WorkMode.Onsite,
            "Čak za firmu dobio preporuku od MarijaB.\nIzvor: pokusaji.txt:85",
            "Izričit odgovor od HR Managerice da trenutno nisu u potrazi za ovim profilom."
        );
        await LinkApplicationContact(appIto, ctSanjaZovko);
        await AddInteraction(
            appIto,
            ctSanjaZovko,
            InteractionDirection.Outbound,
            InteractionChannel.Email,
            new DateTime(2026, 8, 28, 11, 0, 0, DateTimeKind.Utc),
            "Prijava za poziciju u razvojnom timu ITO Mostar",
            "Poštovanje,\n\nZovem se Nedim Jugo, diplomirani bachelor softverskog inženjeringa(Univerzitet „Džemal Bijedić“, Mostar), i javljam se putem ovim putem u potražnji za pozicijom u Vašem razvojnom timu.\n\nIskreno bi volio da ITO bude prva stanica u mojoj karijeri. Kao mostarska firma koja gradi konkretna softverska rješenja kako za sam grad tako i za veće i naprednije nivoe i klijente, teško mogu zamisliti bolje mjesto da započnem svoj profesionalni put kao developer.\n\nU prilogu Vam šaljem svoj CV i motivaciono pismo, sa detaljnijim pregledom dosadašnjeg iskustva i projekata. Stojim na raspolaganju za razgovor u terminu koji vama odgovara.\n\nUnaprijed zahvaljujem na vremenu i razmatranju moje prijave.\n\nS poštovanjem,\nNedim Jugo\n+387 60 318 5869\nnedim.jugoo@gmail.com"
        );
        await AddInteraction(
            appIto,
            ctSanjaZovko,
            InteractionDirection.Inbound,
            InteractionChannel.Email,
            new DateTime(2026, 8, 29, 13, 15, 0, DateTimeKind.Utc),
            "Odgovor na prijavu - Sanja Zovko",
            "Poštovani,\n\nzahvaljujemo se na Vašem javljanju.\n\nTrenutno nismo u potrazi za nekim vaših kvaliteta. Svakako vam želimo sve najbolje u vašem daljnjem radu.\n\nLijep pozdrav,\n\nSANJA ZOVKO  | HR MANAGER\nsanja@ito.ba  | +387 (0) 63 685 886  | +387 (0) 36 830 003  | ito.dev"
        );

        // ==========================================
        // 4. GALEYO MOSTAR (f04)
        // ==========================================
        var cGaleyo = await GetOrCreateCompany("Galeyo (ured u Mostaru)", "https://galeyo.com", "Mostar, BiH", "Software Development");
        var ctNinaH = await GetOrCreateContact("Nina H.", cGaleyo.Id, "Head of Human Potential & Employer Branding", null, null, "https://www.linkedin.com/in/nina-h", ContactType.HiringManager);
        var appGaleyo = await GetOrCreateApplication(
            cGaleyo,
            "General application (Junior Developer)",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc),
            ApplicationSource.CompanyWebsite,
            "forma na stranici (general application) + LinkedIn",
            WorkMode.Onsite,
            "Poslao na general application. Ali sam pisao i Nini na LinkedIn.\nIzvor: pokusaji.txt:127-137",
            "Meka odbijenica: nema pozicije za ovaj profil, kontaktirat će ako se otvori."
        );
        await LinkApplicationContact(appGaleyo, ctNinaH);
        await AddInteraction(
            appGaleyo,
            null,
            InteractionDirection.Inbound,
            InteractionChannel.Email,
            new DateTime(2026, 9, 2, 12, 5, 0, DateTimeKind.Utc),
            "Potvrda prijave na general application",
            "Dear Nedim Jugo,\n\nThank you for your successful application. If your application matches the job description we are looking for, our team will be happy to contact you.\n\nThank you for your application.\n\nBest regards"
        );
        await AddInteraction(
            appGaleyo,
            ctNinaH,
            InteractionDirection.Outbound,
            InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 5, 18, 9, 0, DateTimeKind.Utc),
            "LinkedIn poruka Nini H. povodom prijave",
            "Poštovanje, znam da je subota i da ne radite tako da ne očekujem tokom vikenda ni odgovor. Prvo bi volio da se predstavim, ja sam Nedim Jugo bachelor softverskog inženjeringa, trenutno sam u potrazi za poslom i po mogućnosti nekom početnom pozicijom koja bi mi dala iskustvo, priliku za učenje ali i mogućnost da sudjelujem sa timom u projektima i developmentu. Prije nekoliko dana kroz vašu formu na stranici sam poslao prijavu gdje sam unio svoje podatke, CV i motivaciono. Galeyo me privlači jer sam čuo samo dobre stavari o vašoj firmi, ali i također jer imate ured u Mostaru pa ne bi bio prinuđen na selidbu. Ono zbog čega vam se javljam jeste kako bi vidio da li ima mogućnosti da u sljedećoj sedmici pogledate moj CV i motivaciono i kolike su mogućnosti rada u vašim timovima. Ja bi svakako bio otvoren za bilo kakav razgovor u terminu kada vama odgovara. Nadam se da vam neće smetati to što vam se sada javljam i što vam se javljam ovim putem, ali unaprijed vam hvala na svakom odgovoru i pomoći."
        );
        await AddInteraction(
            appGaleyo,
            ctNinaH,
            InteractionDirection.Inbound,
            InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 7, 10, 54, 0, DateTimeKind.Utc),
            "Odgovor Nine H. na LinkedInu",
            "prije svega želim da se zahvalim na lijepim riječima i vašem interesu za kompaniju Galeyo. Mi ćemo svakako biti slobodni da vas pozovemo ukoliko se otvori neko radno mjesto koje bi odgovaralo vašem profesionalnom profilu.\n\nLijep pozdrav,\nNina"
        );
        await AddInteraction(
            appGaleyo,
            ctNinaH,
            InteractionDirection.Outbound,
            InteractionChannel.LinkedIn,
            new DateTime(2026, 9, 7, 11, 25, 0, DateTimeKind.Utc),
            "Zahvala Nini na odgovoru",
            "Hvala Vam na razmatranju moje prijave. Ono što Vam ja mogu garatovati, ukoliko bude izabran za člana Vašeg tima, je prilagođavanje i poštovanje Vaših standarda i dodatni razvoj i rad. Žao mi je što trenutno ne postoji pozicija za moj profil, ali hvala Vama na javljanju i odgovoru, u svakom slučaju ostajem dostupan za daljnji razgovor i mogući intervju gdje ćete imati priliku vidjeti i upoznati mene kako u tehničkom tako i društvenom karakteru.\n\nUgodan dan Vam želim"
        );

        // ==========================================
        // 5. HTEC GROUP MOSTAR (f05)
        // ==========================================
        var cHtec = await GetOrCreateCompany("HTEC Group (ured Mostar)", "https://htecgroup.com", "Mostar, BiH", "IT Engineering & Consulting");
        var ctDamirAvdic = await GetOrCreateContact("Damir Avdić", cHtec.Id, "Embedded Software Engineer (Automotive), PhD Candidate", null, null, "https://www.linkedin.com/in/damir-avdic", ContactType.Peer, "Ponudio se kao veza prema TA timu");
        var appHtec = await GetOrCreateApplication(
            cHtec,
            "Junior Software Developer / Junior Project Manager",
            ApplicationStatus.Ghosted,
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            ApplicationSource.Other,
            "email: office-mo@htecgroup.com + LinkedIn kontakt",
            WorkMode.Hybrid,
            "Slao na mejl u Mostaru niko nije nikad odgovorio office-mo@htecgroup.com. Javio sam se i Damiru na LinkedIn.\nSavjet: pratiti web i kanale HTEC-a, direktno pisati TA timu na LinkedInu.\nIzvor: pokusaji.txt:175-194",
            "Na email niko nije odgovorio. Preko internog inženjera saznao da trenutno nema junior rola."
        );
        await LinkApplicationContact(appHtec, ctDamirAvdic);
        await AddInteraction(
            appHtec,
            null,
            InteractionDirection.Outbound,
            InteractionChannel.Email,
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            "Prijava za posao - HTEC Office Mostar",
            "Poštovani,\n\nobraćam Vam se sa interesovanjem za mogućnosti zaposlenja u HTEC-u, odnosno u Vašem uredu u Mostaru.\n\nNedavno sam završio Bachelor studij Softverskog inženjeringa i trenutno tražim priliku za početak profesionalne karijere. Najviše se pronalazim u pozicijama Junior Software Developera ili Junior Project Managera, gdje bih mogao iskoristiti svoje tehničko znanje, iskustvo na projektima i interes za organizaciju i vođenje projekata.\n\nIpak, otvoren sam i za druge junior pozicije za koje smatrate da bi odgovarale mom profilu i vještinama. Rado bih razgovarao o dostupnim mogućnostima i čuo više o tome gdje bih mogao najbolje doprinijeti Vašem timu, ali i naučiti nove stvari od Vaših mentora.\n\nU prilogu dostavljam svoj CV i motivaciono pismo, a ukoliko postoji odgovarajuća prilika, bio bih zahvalan za mogućnost kratkog razgovora.\n\nHvala Vam na vremenu i razmatranju moje prijave.\n\nS poštovanjem,\nNedim Jugo\n+387 60 318 5869\nnedim.jugoo@gmail.com"
        );
        await AddInteraction(appHtec, ctDamirAvdic, InteractionDirection.Outbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 4, 15, 21, 0, DateTimeKind.Utc), "Upit Damiru Avdiću o junior pozicijama", "Pozdrav Damire, dok sam listao LinkedIn imao sam priliku naići na vašu nedavnu objavu o zapošljavanju u HTEC-u i da ste se ponudili da budete taj gateway da TA, pa nadam se da vam inbox nije pun al u nadi da ćete vidjeti moju poruku. Ukratko završio sam 4. godine faksa i softverski inženjer sam, sad tražim neku početnu poziciju kako bi krenuo sa karijerom. Prije nekoliko dana na vas HTEC office Mostar sam poslao svoj CV i motivaciono pismo kao nadu da ću možda uspjeti dobiti neku poziciju. Ono što mene zanima je da li biste bili zainteresovani za neke pozicije juniora i koliko je to moguće u Mostaru i ako \"da\" biste li mogli nekome možda sugerisati da pogleda moj mejl. Naravno unaprijed vam hvala");
        await AddInteraction(appHtec, ctDamirAvdic, InteractionDirection.Inbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 4, 15, 34, 0, DateTimeKind.Utc), "Odgovor Damira o stanju sa junior rolama", "Za sada je otvoreno samo to sto je na webu, mislim da junior rola nema ali to je samo trenutno.\nVjerujem da su kolege zaprimile CV i motivaciono i da ako se ukaze pozicija da ce te zvati.\nPrati web, i kanale HTECa ako se nesto otvori gdje mozes konkurisati - pingaj mene pa cu ja da vidim sta je sa CV-jem.");
        await AddInteraction(appHtec, ctDamirAvdic, InteractionDirection.Outbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 4, 15, 36, 0, DateTimeKind.Utc), "Zahvala Damiru i praćenje kanala", "Hvala svakako na javljanju. Pratit ću stranice, MSM kao što i do sad radim. Vidim da su prilike za nas male ali eto mislio sam svakako da vrijedi pokušati poslati a i napisati Vama pa šta bude bude");
        await AddInteraction(appHtec, ctDamirAvdic, InteractionDirection.Inbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 4, 15, 39, 0, DateTimeKind.Utc), "Savjet o krizi outsourcinga i TA timu", "A nazalost znam da je problem juniorima u danasnje vrijeme jer je kriza pogodila outsourcing kompanije a ondao dosao i AI.\nTrebalo bi da se ustabili do kraja godine, probaj i na Linkedinu da filtriras poslove i da vidis gdje mozes upast.\nBolje ti je javi se direktno TA/Recruiting timu na Linkedinu nego da saljes CV :)");
        await AddInteraction(appHtec, ctDamirAvdic, InteractionDirection.Outbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 4, 15, 43, 0, DateTimeKind.Utc), "Odgovor o stanju na tržištu", "Svakako i to radim javljam se al nekako sad za sad ima dva mjeseca samo nailazim na odgovore nema. Al opet nije ovo vas problem pa da slušate a ako vi imali neku poziciju i nešto ako se slučajno sjetite mene ja sam dostupan");
        await AddInteraction(appHtec, ctDamirAvdic, InteractionDirection.Inbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 4, 15, 47, 0, DateTimeKind.Utc), "Motivacijska poruka od Damira", "Ja sam se zaposljavao u zlatnom dobu ITa pa sam 4-5 mjeseci trazio posao, cisto da znas da nije tako crno, doci ce :)");
        await AddInteraction(appHtec, ctDamirAvdic, InteractionDirection.Outbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 4, 16, 0, 0, DateTimeKind.Utc), "Zahvala na motivaciji", "Hvala na motivaciji i lijepim rijecima");

        // ==========================================
        // 6. SAAS SOLUTIONS MOSTAR (f06)
        // ==========================================
        var cSaasSolutions = await GetOrCreateCompany("SaaS Solutions (powered by mih GmbH, ured u Mostaru)", "https://saasolutions.ba", "Mostar, BiH", "Software Development");
        var ctEminaCehajic = await GetOrCreateContact("Emina Cehajic", cSaasSolutions.Id, "Head of HR", null, null, "https://www.linkedin.com/in/emina-cehajic", ContactType.HiringManager);
        var ctMatejaSumic = await GetOrCreateContact("Mateja Šumić", cSaasSolutions.Id, "IT Recruiter", "mateja.sumic@sqa-consulting.com", null, null, ContactType.Recruiter, "SQA Consulting / SaaS Solutions HR");
        var ctNihad = await GetOrCreateContact("Nihad", cSaasSolutions.Id, "Direktor", null, null, null, ContactType.Other, "Upoznat lično na FITCC-u");

        var appSaasSolutions = await GetOrCreateApplication(
            cSaasSolutions,
            "Junior developer (samoinicijativna prijava)",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 2, 17, 50, 0, DateTimeKind.Utc),
            ApplicationSource.Other,
            "email + LinkedIn + HR email",
            WorkMode.Onsite,
            "Slao na mejl niko nije odgovorio. Onda sam se javio Emini na LinkedIn, i onda sam se javio i Mateji na mejl.\nIshod: Mateja Šumić odgovorila da trenutno nema otvorenih pozicija za junior developere, CV je sačuvan u njihovoj bazi za buduće prilike.\nIzvor: pokusaji.txt:245-335",
            "Recruiter izričito: nema otvorenih junior pozicija. CV ostaje u bazi."
        );
        await LinkApplicationContact(appSaasSolutions, ctEminaCehajic);
        await LinkApplicationContact(appSaasSolutions, ctMatejaSumic);

        await AddInteraction(appSaasSolutions, null, InteractionDirection.Outbound, InteractionChannel.Email, new DateTime(2026, 9, 2, 17, 50, 0, DateTimeKind.Utc), "Samoinicijativna prijava na careers@saasolutions.ba", "Poštovani,\n\nobraćam Vam se sa željom da se prijavim za neku od dostupnih junior pozicija u SaaS Solutions.\n\nNedavno sam završio Bachelor studij Softverskog inženjerstva i trenutno tražim priliku za početak profesionalne karijere. Posebno me zanimaju pozicije vezane za software development i projekt menadžment, ali sam otvoren i za druge pozicije koje odgovaraju mom znanju, iskustvu i interesovanjima.\n\nVidjevši da ste otvorili ured u Mostaru, posebno mi je bilo interesantno da istražim mogućnost pridruživanja Vašem timu upravo na ovoj lokaciji.\n\nPrijavu šaljem putem e-maila jer sam želio dostaviti i motivaciono pismo, što nisam bio u mogućnosti učiniti putem online forme. U prilogu se nalaze moj CV i motivaciono pismo.\n\nBio bih veoma zainteresovan za priliku da razgovaramo o eventualnim otvorenim pozicijama i mogućnostima za moj profil. Spreman sam razmotriti i druge uloge ukoliko smatrate da bih se bolje uklopio u neku drugu poziciju u Vašem timu.\n\nHvala Vam na vremenu i razmatranju moje prijave.\n\nS poštovanjem,\nNedim Jugo\n+387 60 318 5869\nnedim.jugoo@gmail.com");
        await AddInteraction(appSaasSolutions, ctEminaCehajic, InteractionDirection.Outbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 10, 13, 26, 0, DateTimeKind.Utc), "Poruka Emini Ćehajić na LinkedInu", "Poštovanje Emina,\n\nNadam se da vam ne smeta što vam se javljam ovdje, prije nekoliko dana sam na mejl careers@saasolutions.ba poslao samoonicijativnu prijavu za junior dev poziciju u SaaSSolutions, pa bi i ovom prilikom htio malo objasniti zašto mi se baš vaša kompanija čini kao dobar start i boost, ali i možda tražiti feedback ako je to moguće.\n\nPošto živim u Mostaru i prije nekoliko mjeseci vidio sam da imate office i ovdje, što mi je bilo lijepo iznenađenje. Prošle godine sam imao priliku porazgovarati s Vašim direktorom gospodinom Nihadom na FITCC-u gdje mi je on no predstavio kompaniju i tehnologije, i tom prilikom sam primijetio koliko se tehnologije s kojima radite poklapaju s onim na čemu ja radim - ASP.NET Core, Angular, React, SQL Server, plus nekoliko full-stack projekata koje sam sam vodio od ideje do produkcije.\n\nBilo bi mi drago ako biste mogli pogledati moju prijavu kad budete imali priliku i javiti mi ima li trenutno u Vašem timu prostora za nekoga s mojim profilom. Ono što ja još mogu garantovati je moje puno prilagođavanje Vašim standardima, ali i vidim tu mogućnost mog daljeg učenja i napredovanja. Otvoren sam za razgovor kad god Vama odgovara.\n\nHvala Vam puno i lijep pozdrav,\nNedim");
        await AddInteraction(appSaasSolutions, ctEminaCehajic, InteractionDirection.Inbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 10, 14, 23, 0, DateTimeKind.Utc), "Odgovor Emine sa uputom za Mateju Šumić", "Dragi Nedime, hvala na javljanju i interesu za moguću saradnju! Zamolila bih te da se javiš kolegici Mateji Sumic koja je u HR regrutacijskim procesima.\nSvako dobro!");
        await AddInteraction(appSaasSolutions, ctEminaCehajic, InteractionDirection.Outbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 10, 14, 55, 0, DateTimeKind.Utc), "Potvrda slanja zahtjeva Mateji", "Mnogo hvala na pročitanoj poruci i odgovoru, poslao sam zahtjev Vašoj kolegici i pišem uskoro. Ugodan dan(Edited)");
        await AddInteraction(appSaasSolutions, ctEminaCehajic, InteractionDirection.Outbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 14, 10, 55, 0, DateTimeKind.Utc), "Upit Emini za alternativni kontakt za Mateju", "Poštovanje, izvinjavam se što se ponovo javljam. Vašoj kolegici sam u četvrtak poslao zahtjev na LinkedIn-u, ali pošto mi nije još odobrila nisam joj u mogućnosti napisati poruku. Zanima me samo znate li slučajno da kolegica nije na odmoru ili ako vi imate bolji način da je kontaktiram.");
        await AddInteraction(appSaasSolutions, ctEminaCehajic, InteractionDirection.Inbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 14, 11, 35, 0, DateTimeKind.Utc), "Emina šalje email adresu od Mateje Šumić", "Cao cao, saljem mail;)\nmateja.sumic@sqa-consulting.com");
        await AddInteraction(appSaasSolutions, ctMatejaSumic, InteractionDirection.Outbound, InteractionChannel.Email, new DateTime(2026, 9, 14, 11, 43, 0, DateTimeKind.Utc), "Prijava za posao - Mateja Šumić", "Poštovanje Mateja,\n\nU četvrtak sam se javio Vašoj kolegici Emini Ćehajić koja me uputila da se obratim Vama vezano za moju prijavu - prije nekoliko dana sam na mejl careers@saasolutions.ba poslao samoinicijativnu prijavu za junior dev poziciju u SaaSSolutions, pa bih i ovom prilikom htio malo objasniti zašto mi se baš vaša kompanija čini kao dobar start i boost, ali i možda tražiti feedback ako je to moguće.\n\nPošto živim u Mostaru i prije nekoliko mjeseci vidio sam da imate office i ovdje, što mi je bilo lijepo iznenađenje. Prošle godine sam imao priliku porazgovarati s Vašim direktorom gospodinom Nihadom na FITCC-u gdje mi je on predstavio kompaniju i tehnologije, i tom prilikom sam primijetio koliko se tehnologije s kojima radite poklapaju s onim na čemu ja radim - ASP.NET Core, Angular, React, SQL Server, plus nekoliko full-stack projekata koje sam sam vodio od ideje do produkcije.\n\nBilo bi mi drago ako biste mogli pogledati moju prijavu kad budete imali priliku i javiti mi ima li trenutno u Vašem timu prostora za nekoga s mojim profilom. Ono što ja još mogu garantovati je moje puno prilagođavanje Vašim standardima, ali i vidim tu mogućnost mog daljeg učenja i napredovanja. Otvoren sam za razgovor kad god Vama odgovara. Ukoliko je potrebno da i ovdje pošaljem CV i motivaciono nije nikakav problem.\n\nHvala Vam puno i lijep pozdrav,\nNedim");
        await AddInteraction(appSaasSolutions, ctEminaCehajic, InteractionDirection.Outbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 14, 11, 44, 0, DateTimeKind.Utc), "Obavijest Emini da je email poslan", "Hvala ponovo, mejl već poslan");
        await AddInteraction(appSaasSolutions, ctMatejaSumic, InteractionDirection.Inbound, InteractionChannel.Email, new DateTime(2026, 9, 15, 12, 49, 0, DateTimeKind.Utc), "Odgovor Mateje Šumić - CV u bazu", "Pozdrav Nedim,\n\nHvala Vam puno na javljanju i Vašem interesu za našu kompaniju.\nNažalost trenutno nemamo otvorenih pozicija za junior developere, ali bismo svakako željeli da imamo Vaš CV u našoj bazi ako se situacija promijeni i otvori se neka pozicija na koju biste nam se uklapali.\n\nStoga Vas molim ako možete da pošaljete Vaš CV.\n\nHvala Vam još jednom na interesu za našu kompaniju.\n\nSrdačan pozdrav,\n\nMateja Šumić\nIT Recruiter");
        await AddInteraction(appSaasSolutions, ctMatejaSumic, InteractionDirection.Outbound, InteractionChannel.Email, new DateTime(2026, 9, 15, 12, 54, 0, DateTimeKind.Utc), "Slanje CV-ja Mateji Šumić", "Poštovana Mateja,\n\nHvala Vam na odgovoru.\n\nU prilogu Vam dostavljam svoj CV. Iako razumijem da trenutno nema otvorenih pozicija za junior developere, volio bih Vas zamoliti da svakako pogledate moj profil kada budete u prilici.\n\nIzuzetno sam motivisan za rad u Vašem timu, posebno u mostarskom uredu, te sam potpuno otvoren i voljan razmotriti bilo koje druge uloge ili prilike (uključujući srodne tehničke pozicije ili projekt menadžment) ukoliko procijenite da bih svojim vještinama i profilom mogao odgovarati trenutnim ili skorim potrebama.\n\nStojim Vam na raspolaganju za kratak razgovor ili bilo kakve dodatne informacije.\n\nHvala Vam još jednom na izdvojenom vremenu.\n\nSrdačan pozdrav,\nNedim Jugo\n+387 60 318 5869\nnedim.jugoo@gmail.com");
        await AddInteraction(appSaasSolutions, ctMatejaSumic, InteractionDirection.Inbound, InteractionChannel.Email, new DateTime(2026, 9, 15, 13, 21, 0, DateTimeKind.Utc), "Potvrda prijema CV-ja od Mateje Šumić", "Pozdrav Nedim,\n\nNema na čemu, hvala Vama na brzom odgovoru i slanju.\nPogledamo svakako Vaš CV detaljnije, te Vas kontaktiramo ako se otvori neka prilika.\n\nHvala Vam još jednom i bit ćemo u kontaktu za dalje.\n\nSrdačan pozdrav,\nMateja Šumić\nIT Recruiter");

        // ==========================================
        // 7. SOFTRAY SOLUTIONS SARAJEVO (f07)
        // ==========================================
        var cSoftray = await GetOrCreateCompany("Softray Solutions (Sarajevo)", "https://www.softraysolutions.com", "Sarajevo, BiH", "Software Development");
        var appSoftray = await GetOrCreateApplication(
            cSoftray,
            "Otvorena prijava, razvojni tim Sarajevo",
            ApplicationStatus.Ghosted,
            new DateTime(2026, 9, 4, 16, 38, 0, DateTimeKind.Utc),
            ApplicationSource.CompanyWebsite,
            "email: careers@softraysolutions.com (general application)",
            WorkMode.Hybrid,
            "Slao na general application careers@softraysolutions.com. Dobio samo automatizirani mejl.\nIzvor: pokusaji.txt:449-472",
            "Samo automatska potvrda, bez povratne informacije."
        );
        await AddInteraction(appSoftray, null, InteractionDirection.Outbound, InteractionChannel.Email, new DateTime(2026, 9, 4, 16, 38, 0, DateTimeKind.Utc), "Otvorena prijava za poziciju u razvojnom timu", "Poštovani,\n\nZovem se Nedim Jugo, diplomirani inženjer softvera (Univerzitet „Džemal Bijedić“, Mostar), i javljam se putem otvorene aplikacije za poziciju u vašem razvojnom timu u Sarajevu.\n\nDo sada sam radio na nekoliko projekata u tehnologijama koje i vi koristite - Angular, ASP.NET Core, Azure - pa smatram da bih se brzo uklopio u tim i počeo doprinositi projektima za vaše klijente, ali i da bi Vaša ekipa mogla pomoći meni da bolje radim i više naučim.\n\nU prilogu vam šaljem CV i motivaciono pismo, sa detaljnijim pregledom dosadašnjeg iskustva i projekata. Stojim na raspolaganju za razgovor u terminu koji vama odgovara.\n\nUnaprijed zahvaljujem na vremenu i razmatranju moje prijave.\n\nS poštovanjem,\nNedim Jugo\n+387 60 318 5869\nnedim.jugoo@gmail.com");
        await AddInteraction(appSoftray, null, InteractionDirection.Inbound, InteractionChannel.Email, new DateTime(2026, 9, 4, 16, 38, 30, DateTimeKind.Utc), "Automatski odgovor - Softray Careers", "Thank you for expressing an interest in employment with Softray Solutions. Our HR department will review your application and get back to you in case you are selected to continue to the interview process.\n\nIf you do not hear from us, it is likely that we have identified a candidate for the position or the position is no longer active. Softray Solutions greatly appreciates your interest and would like to encourage you to periodically refer to www.softraysolutions.com/careers for the list of our current openings.\n\nThank you for your interest and we wish you success in your job search.\n\nRegards,\n\nSoftray Team\nwww.softraysolutions.com");

        // ==========================================
        // 8. ZIRA GROUP (f08)
        // ==========================================
        var cZira = await GetOrCreateCompany("ZIRA Group", "https://ziragroup.com", "Sarajevo, BiH", "Telecom Software & BSS");
        var ctNedimSalahovic = await GetOrCreateContact("Nedim Salahović", cZira.Id, "People Partner", null, null, "https://www.linkedin.com/in/nedim-salahovic", ContactType.HiringManager);
        var ctArminBabovic = await GetOrCreateContact("Armin Babović", cZira.Id, "Talent Acquisition Specialist", "armin.babovic@ziragroup.com", null, "https://www.linkedin.com/in/armin-babovic", ContactType.Recruiter);
        var ctMedina = await GetOrCreateContact("Medina", cZira.Id, "Član ZIRA tima (ZTA kontakt)", null, null, null, ContactType.Other);

        var appZira = await GetOrCreateApplication(
            cZira,
            "Open Application (junior developer / junior PM)",
            ApplicationStatus.Applied,
            new DateTime(2026, 9, 14, 10, 0, 0, DateTimeKind.Utc),
            ApplicationSource.CompanyWebsite,
            "forma (Open Application) + LinkedIn",
            WorkMode.Hybrid,
            "Zira javio se preko stranice forme dobio automatizirani mejl. Pisao Nedimu pa Arminu Baboviću na LinkedIn.\nArmin Babović: trenutno nema otvorenih pozicija, juniori dolaze isključivo kroz Dev ZTA (3 mjeseca rada, evaluacija za stalni angažman). Dobro spremiti prijavu, javiti se Medini iz tima, poslati projekte.\nIzvor: pokusaji.txt:492-552",
            null
        );
        await LinkApplicationContact(appZira, ctNedimSalahovic);
        await LinkApplicationContact(appZira, ctArminBabovic);

        await AddInteraction(appZira, ctArminBabovic, InteractionDirection.Inbound, InteractionChannel.Email, new DateTime(2026, 9, 14, 10, 5, 0, DateTimeKind.Utc), "Potvrda otvorene aplikacije - Armin Babović, ZIRA", "Dear Nedim.\n\nThank you for submitting your application and for considering ZIRA as part of your career journey.\n\nWe know that every application represents much more than a CV.\n\nBehind it are years of learning, professional experiences, personal ambitions, career goals, and often the hope that the next opportunity could lead to something meaningful. That's why we never take an application for granted, and we're grateful that you chose to share yours with us.\n\nWhat happens next?\n\nYou've applied through our Open Application form.\n\nThis means your profile will be available to our recruitment team for future opportunities. Because this is not an application for a specific open position, there may or may not be a role that matches your experience and aspirations at this moment.\n\nHowever, if an opportunity arises that aligns with your profile, we'll be happy to reach out and continue the conversation...\n\nBest regards,\nArmin Babović\nTalent Acquisition Specialist\nZIRA Group\nLinkedIn");
        await AddInteraction(appZira, ctNedimSalahovic, InteractionDirection.Outbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 15, 10, 41, 0, DateTimeKind.Utc), "Poruka Nedimu Salahoviću na LinkedInu", "Poštovanje Nedime,\n\nNadam se da ste dobro i da Vam ne smeta inicijativa što Vam pišem ovim putem. Javljam Vam se jer već duže vrijeme pratim rad Zire, od FIT-CCa i priča sa Vašim ljudima pa sve do danas kad slavite 30 godina, a nekoliko kolega i kolegica koji rade kod Vas prenijeli su mi izuzetno pozitivna iskustva o atmosferi, timu i radnoj kulturi.\n\nZavršio sam softverski inžinjering na FIT-u u Mostaru. Kroz fakultet, praktičan rad i nagrađivane projekte na takmičenjima, stekao sam čvrste osnove u full-stack razvoju (.NET, C#,SQL, Angular, React, Flutter). Također, kroz vođenje timskih projekata i organizaciju različitih inicijativa razvio sam izražene organizacijske i komunikacijske vještine, pa me podjednako privlače i razvoj softvera i koordinacija projekata.\n\nVeć sam poslao svoj CV i motivaciono pismo putem Vaše zvanične forme za prijavu. Znam da je i teško i nezahvalno kao prvi posao tražiti specifične pozicije, ali bitno je da znate da se najkomotnije osjećam na pozicijama Junior Software Developera ili Junior Project Managera (PM), ali sam potpuno otvoren i za druge juniorske prilike ili Vaše prijedloge ukoliko smatrate da bih se negdje drugo bolje uklopio.\n\nBio bih Vam zahvalan ako ugrabite malo vremena da pogledate moju prijavu, a rado bih se i detaljnije predstavio ukoliko bude prilike za razgovor.\n\nHvala Vam na vremenu i lijep pozdrav!");
        await AddInteraction(appZira, ctNedimSalahovic, InteractionDirection.Inbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 18, 11, 19, 0, DateTimeKind.Utc), "Nedim Salahović upućuje na Armina Babovića", "Pozdrav Nedime, izvinjavam se na zakašnjelom odgovoru, nekako sam propustio poruku :) molim Vas, javite se našem TAS kolegi Arminu Baboviću, možete i na LinkedInu, možete i putem maila armin.babovic@ziragroup.com; on će biti u prilici da Vam da najkonkretnije informacije.\n\nPuno pozdrava i sreće!");
        await AddInteraction(appZira, ctNedimSalahovic, InteractionDirection.Outbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 18, 11, 46, 0, DateTimeKind.Utc), "Odgovor Nedimu Salahoviću", "Zdravo Nedime, nema veze što je zakasnio malo ali makar je odgovor. Ja sam svakako Vašem kolegi poslao connection request na LinkedIn pa mi šaljem i poruku kad prihvati, a ako je negdje oko Vas bio bi zahvalan samo da mu napomenete da prihvati request samo kako bi komunikacija tekla brzim tokom. A ukoliko nto ne bude u nekih 1h vremena svakako mu šaljem i mejl. Hvala svakako na infomracijama(Edited)");
        await AddInteraction(appZira, ctArminBabovic, InteractionDirection.Outbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 18, 11, 52, 0, DateTimeKind.Utc), "Prijava i predstavljanje Arminu Baboviću", "Poštovanje Armine,\n\nPisao sam Vašem kolegi Nedimu prije nekoliko dana, a on me danas uputio da se javim Vama. Nadam se da ste dobro i da Vam ne smeta inicijativa što Vam pišem ovim putem. Javljam Vam se jer već duže vrijeme pratim rad Zire, od FIT-CCa i priča sa Vašim ljudima pa sve do danas kad slavite 30 godina, a nekoliko kolega i kolegica koji rade kod Vas prenijeli su mi izuzetno pozitivna iskustva o atmosferi, timu i radnoj kulturi.\n\nZavršio sam softverski inžinjering na FIT-u u Mostaru. Kroz fakultet, praktičan rad i nagrađivane projekte na takmičenjima, stekao sam čvrste osnove u full-stack razvoju (.NET, C#,SQL, Angular, React, Flutter). Također, kroz vođenje timskih projekata i organizaciju različitih inicijativa razvio sam izražene organizacijske i komunikacijske vještine, pa me podjednako privlače i razvoj softvera i koordinacija projekata.\n\nVeć sam poslao svoj CV i motivaciono pismo putem Vaše zvanične forme za prijavu. Znam da je i teško i nezahvalno kao prvi posao tražiti specifične pozicije, ali bitno je da znate da se najkomotnije osjećam na pozicijama Junior Software Developera ili Junior Project Managera (PM), ali sam potpuno otvoren i za druge juniorske prilike ili Vaše prijedloge ukoliko smatrate da bih se negdje drugo bolje uklopio.\n\nBio bih Vam zahvalan ako ugrabite malo vremena da pogledate moju prijavu, a rado bih se i detaljnije predstavio ukoliko bude prilike za razgovor.\n\nHvala Vam na vremenu i lijep pozdrav!");
        await AddInteraction(appZira, ctArminBabovic, InteractionDirection.Inbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 22, 16, 2, 0, DateTimeKind.Utc), "Armin Babović predlaže Dev ZTA program", "Pozdrav Nedime, drago mi je da si pisao.\n\nTrenutno nemamo otvorenih pozicija, ali možeš se prijaviti na Dev ZTA koji će uskoro biti aktivan.\n\nMislim da je tako najbolje, i onda će kolege da procijene tvoj profil i ako sve bude okej zvat će te. To je internship koji može zaposliti zaposlenjem u ZIRI.\n\nNa taj način možeš krenuti kao software developer");
        await AddInteraction(appZira, ctArminBabovic, InteractionDirection.Outbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 22, 16, 28, 0, DateTimeKind.Utc), "Odgovor u vezi ZTA i stalnog posla", "Armin: Pozdrav Nedime, drago mi je da si pisao. Trenutno nemamo otvorenih pozicija, ali možeš se prijaviti na Dev ZTA koji će uskoro biti aktivan.\nHvala na javljanju Armine, žao mi je što u vašim timovima nema mjesta za jednog juniora, ali hvala na informaciji. Pratit ću svakako i ZTA, a trenutno sam stvarno u potrazi za nekim permanentnim zaposlenjem, jer onako kad završiš faks vrijeme je za takvo nešto.");
        await AddInteraction(appZira, ctArminBabovic, InteractionDirection.Inbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 22, 16, 33, 0, DateTimeKind.Utc), "Armin objašnjava proces zapošljavanja juniora", "Pa ima mjesta kroz taj ZTA, tako se kod nas zapošljavaju juniori.\n\nTako da, dobro spremi prijavu, javi se slobodno Medini iz našeg tima, pošalji neke svoje projekte.\n\nSvi juniori u firmi dolaze kroz ZTA, ima 3 mjeseca praktičnog rada i onda se na kraju dolazi do odluke ko će ostati kao stalni član tima.");
        await AddInteraction(appZira, ctArminBabovic, InteractionDirection.Outbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 22, 16, 53, 0, DateTimeKind.Utc), "Prihvaćen savjet i plan za ZTA", "Hvala na preporukama, prijavim se na ZTA pa vidim kako i to prođe.");

        // ==========================================
        // 9. MANPOWER BIH (f09)
        // ==========================================
        var cManpower = await GetOrCreateCompany("Manpower Bosna i Hercegovina", "https://manpowersee.com", "Sarajevo / BiH", "Recruitment Agency");
        var appManpower = await GetOrCreateApplication(
            cManpower,
            "Call for IT professionals (baza kandidata)",
            ApplicationStatus.Applied,
            new DateTime(2026, 9, 4, 18, 8, 0, DateTimeKind.Utc),
            ApplicationSource.Other,
            "online forma: 'Call for IT professionals'",
            WorkMode.Remote,
            "Poslao ovdje prijavu u IT bazu. Oni nisu firma nego više kao agencija ako bude posla zovnu.\nPodaci iz forme: Iskustvo: 1 god, Očekivanje plate: 1750, Relokacija: Ne, Otkazni rok: 8 dana.\nIzvor: pokusaji.txt:656",
            null
        );
        await AddInteraction(
            appManpower,
            null,
            InteractionDirection.Inbound,
            InteractionChannel.Email,
            new DateTime(2026, 9, 4, 18, 10, 0, DateTimeKind.Utc),
            "Potvrda prijave - Call for IT professionals",
            "Pozdrav Nedim Jugo!\n\nManpower Bosna i Hercegovina Vam se zahvaljuje na prijavi za poziciju:\nCall for IT professionals\nU nastavku su podaci koje ste unijeli u obrazac za prijavu:\n\nIme i prezime: Nedim Jugo\nE-mail adresa: nedim.jugoo@gmail.com\nKontakt telefon: 0603185869\nKoliko godina relevantnog iskustva imate za poziciju na koju se prijavljujete?: 1\nKoja su Vaša finansijska očekivanja?: 1750\nDa li ste spremni za relokaciju?: Ne\nKoliki je Vaš otkazni rok?: 8\nSaglasnost na obradu podataka: Da, Vrijeme: 2026-09-04 18:08\n\nPregledat ćemo Vašu prijavu i kontaktirati Vas ukoliko nam budu potrebne dodatne informacije."
        );

        // ==========================================
        // 10. BH TELECOM D.D. SARAJEVO (f10)
        // ==========================================
        var cBhTelecom = await GetOrCreateCompany("BH Telecom d.d. Sarajevo", "https://www.bhtelecom.ba", "Sarajevo, BiH", "Telecommunications");
        var ctBhTelecomHr = await GetOrCreateContact("Ljudski resursi BH Telecom", cBhTelecom.Id, "Direkcija za ljudske resurse", "ljudski.resursi@bhtelecom.ba", null, null, ContactType.Recruiter);
        var ctSamirOmerovic = await GetOrCreateContact("Samir Omerović", cBhTelecom.Id, "Ljudski resursi (cc)", "Samir.Omerovic@bhtelecom.ba", null, null, ContactType.Other);
        var ctAmilaMerdzanovic = await GetOrCreateContact("Amila Merdžanović", cBhTelecom.Id, "Ljudski resursi (cc)", "Amila.Merdzanovic@bhtelecom.ba", null, null, ContactType.Other);

        var appBhTelecom = await GetOrCreateApplication(
            cBhTelecom,
            "Pripravnik / Stručni saradnik u IT (molba)",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 2, 9, 0, 0, DateTimeKind.Utc),
            ApplicationSource.CompanyWebsite,
            "web prijava (molba)",
            WorkMode.Onsite,
            "Javio se preko stranice, dobio odgovor. Zapošljavanje u javnom sektoru ide isključivo preko javnih oglasa, prijem trenutno obustavljen zaključkom Vlade FBiH.\nIzvor: pokusaji.txt:684",
            "Zapošljavanje isključivo preko javnih konkursa. Moratorij Vlade FBiH."
        );
        await LinkApplicationContact(appBhTelecom, ctBhTelecomHr);
        await LinkApplicationContact(appBhTelecom, ctSamirOmerovic);
        await LinkApplicationContact(appBhTelecom, ctAmilaMerdzanovic);

        await AddInteraction(
            appBhTelecom,
            ctBhTelecomHr,
            InteractionDirection.Inbound,
            InteractionChannel.Email,
            new DateTime(2026, 9, 7, 10, 33, 0, DateTimeKind.Utc),
            "RE: Aplikacija za posao - Nedim Jugo",
            "Poštovani,\n\nZahvaljujemo se na Vašoj molbi za posao dostavljenoj našoj kompaniji. Prijem novog radnika na neodređeno vrijeme u Dioničkom društvu BH Telecom Sarajevo vrši se isključivo putem javnog oglašavanja, a cijeneći činjenicu da je naša kompanija obveznik primjene Uredbe o postupku prijema u radni odnos u javnom sektoru Federacije Bosne i Hercegovine. S tim u vezi, ovim putem Vas pozivamo da u sredstvima javnog informisanja, kao i na našoj web stranici https://www.bhtelecom.ba/karijera/ pratite objavljivanje oglasa za popunu radnih mjesta za koja se traži stručna sprema i znanje koje posjedujete, te da ukoliko budete zainteresovani podnesete prijavu u skladu sa uslovima iz javnog oglasa.\n\nTakođer napominjemo da će Vaša molba biti evidentirana u registar zaprimljenih aplikacija potencijalnih kandidata za zasnivanje radnog odnosa i bit uzeta u razmatranje ukoliko se u Dioničkom društvu BH Telecom Sarajevo ukaže potreba za prijemom radnika na određeno vrijeme, do 6 (šest) mjeseci, a da znanje i vještine koje posjedujete odgovaraju profilu kandidata kojeg tražimo...\n\nS poštovanjem,\nLJUDSKI RESURSI\nBH Telecom d.d. Sarajevo"
        );

        // ==========================================
        // 11. ENDAVA BIH (f11)
        // ==========================================
        var cEndava = await GetOrCreateCompany("Endava (BiH)", "https://www.endava.com", "Sarajevo / Mostar, BiH", "Software Engineering");
        var ctBedirhanRamiz = await GetOrCreateContact("Bedirhan Ramiz", cEndava.Id, "Talent Acquisition Specialist", "Bedirhan.Ramiz@endava.com", "+38761338932", null, ContactType.Recruiter);
        var appEndava = await GetOrCreateApplication(
            cEndava,
            "Junior Developer (Mostar)",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 6, 13, 1, 0, DateTimeKind.Utc),
            ApplicationSource.Other,
            "email: endava.recruitment.bih@endava.com",
            WorkMode.Hybrid,
            "Javio se na mejl sa CV i motivacionim pismom (bosanski i engleski). Odgovor stigao od Bedirhana Ramiza.\nIzvor: pokusaji.txt:758-762",
            "Nemaju prostora za nove kolege u zemlji. CV ostaje u evidenciji."
        );
        await LinkApplicationContact(appEndava, ctBedirhanRamiz);
        await AddInteraction(
            appEndava,
            ctBedirhanRamiz,
            InteractionDirection.Outbound,
            InteractionChannel.Email,
            new DateTime(2026, 9, 6, 13, 1, 0, DateTimeKind.Utc),
            "Prijava za poziciju Junior Developer - Mostar",
            "Poštovani,\n\nZovem se Nedim Jugo i diplomirani sam bachelor softverskog inženjeringa (Fakultet informacijskih tehnologija, Univerzitet „Džemal Bijedić“ u Mostaru). Obraćam vam se sa željom da svoju karijeru započnem kao Junior Developer u vašem timu u Mostaru.\n\nTokom studija i rada na praktičnim projektima fokusirao sam se na razvoj cjelovitih softverskih rješenja:\n\nBackend & API: Razvoj skalabilnih servisa i baza podataka (.NET / C#, SQL Server, REST API, arhitektura zasnovana na mikroservisima/porukama).\n\nFrontend & Mobile: Izrada modernih web i cross-platform aplikacija (Angular, React, Next.js, Flutter).\n\nIntegracije i primijenjeni AI: Praktično iskustvo sa integracijom pametnih servisa, računarskog vida (CV) i IoT/embedded hardverskih rješenja.\n\nKroz samostalan i timski rad na kompleksnim sistemima navikao sam na brz ulazak u novu tehnologiju, rješavanje konkretnih inženjerskih problema i pisanje čistog, održivog koda.\n\nU prilogu vam dostavljam svoj CV i motivaciono pismo (na bosanskom i engleskom jeziku), u kojima se nalazi detaljniji pregled mojih projekata, koda i dosadašnjeg iskustva.\n\nRado bih porazgovarao o tome kako moje vještine mogu doprinijeti vašim trenutnim projektima, u terminu koji vama najviše odgovara.\n\nUnaprijed hvala na izdvojenom vremenu i razmatranju prijave.\n\nS poštovanjem,\nNedim Jugo\n+387 60 318 5869\nnedim.jugoo@gmail.com"
        );
        await AddInteraction(
            appEndava,
            ctBedirhanRamiz,
            InteractionDirection.Inbound,
            InteractionChannel.Email,
            new DateTime(2026, 9, 9, 9, 52, 0, DateTimeKind.Utc),
            "Odgovor - Bedirhan Ramiz (Endava)",
            "Pozdrav Nedime,\n\nhvala ti na javljanju i interesovanju da se pridružiš nama u Endavi.\nInformacije koje su pružio su jako korisne, ali nažalost, trenutak nije odgovarajući.\nTrenutno nemamo prostora za neke nove kolege u našoj zemlji pa ću ti se zbog toga za ovaj put zahvaliti.\nU slučaju da dođe do nekih promjena, biću slobodan da te kontaktiram.\nSretno u narednom periodu!\n\nLijepi pozdravi,\nBedirhan"
        );

        // ==========================================
        // 12. POPCORN RECRUITERS / TALENTOR (f12)
        // ==========================================
        var cPopcorn = await GetOrCreateCompany("Popcorn Recruiters / Talentor Western Balkans", "https://popcornrecruiters.com", "Sarajevo / Adria", "IT Staffing & Recruitment");
        var ctSarahPuric = await GetOrCreateContact("Sarah Puric", cPopcorn.Id, "Recruitment Consultant", "sarah@popcornrecruiters.com", null, null, ContactType.Recruiter);
        var appPopcorn = await GetOrCreateApplication(
            cPopcorn,
            "Software Engineering Talent Pool (prijava)",
            ApplicationStatus.Applied,
            new DateTime(2026, 9, 28, 14, 0, 0, DateTimeKind.Utc),
            ApplicationSource.Other,
            "prijava na otvorenu poziciju / agencijsku bazu",
            WorkMode.Remote,
            "Oni nisu firma javio se da me imaju u bazi, dobio odgovor od Sarah Purić.\nIzvor: pokusaji.txt:841",
            null
        );
        await LinkApplicationContact(appPopcorn, ctSarahPuric);
        await AddInteraction(
            appPopcorn,
            ctSarahPuric,
            InteractionDirection.Inbound,
            InteractionChannel.Email,
            new DateTime(2026, 9, 29, 21, 0, 0, DateTimeKind.Utc),
            "Potvrda prijave na otvorenu poziciju - Talentor",
            "Pozdrav!\n\nHvala Vam na prijavi na otvorenu poziciju!\n\nVaša prijava je uspješno zaprimljena i trenutno je u procesu pregleda. Javit ćemo Vam se ukoliko Vaš profil odgovara zahtjevima pozicije i pozvati u sljedeću fazu selekcije.\n\nSrdačan pozdrav,\n\nTalentor Western Balkans"
        );

        // ==========================================
        // 13. PORT8 D.O.O. MOSTAR (f13)
        // ==========================================
        var cPort8 = await GetOrCreateCompany("Port8 d.o.o. (Mostar)", "https://port8.ba", "Mostar, BiH", "PropTech / Software");
        var ctPort8Careers = await GetOrCreateContact("Port8 Careers", cPort8.Id, "Talent Team", "careers@port8.ba", null, null, ContactType.Recruiter);
        var appPort8 = await GetOrCreateApplication(
            cPort8,
            "Junior developer (spontana prijava)",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 8, 15, 2, 0, DateTimeKind.Utc),
            ApplicationSource.CompanyWebsite,
            "forma na port8.ba (careers)",
            WorkMode.Onsite,
            "Javio se preko forme dobio odgovor. PropTech startup u vlasništvu emonitor AG.\nIzvor: pokusaji.txt:864",
            "Nemaju otvorenu poziciju koja odgovara kompetencijama. CV zadržan u arhivi."
        );
        await LinkApplicationContact(appPort8, ctPort8Careers);
        await AddInteraction(
            appPort8,
            ctPort8Careers,
            InteractionDirection.Outbound,
            InteractionChannel.Other,
            new DateTime(2026, 9, 8, 15, 2, 0, DateTimeKind.Utc),
            "port8.ba careers application from Nedim Jugo",
            "From: Nedim Jugo\nEmail: nedim.jugoo@gmail.com\nPhone Number: 0603185869\n\nCover Letter:\nNedim Jugo\n+387 60 318 5869 | nedim.jugoo@gmail.com | Mostar, BiH\nLinkedIn: linkedin.com/in/nedim-jugo-492b99277 | GitHub: github.com/NedimJugo | Portfolio: nedim-jugo.vercel.app\n\nPort8 d.o.o. Mostar\n\nPoštovani Port8 tim,\nZovem se Nedim Jugo, bachelor softverskog inženjeringa sa Univerziteta „Džemal Bijedić\" u Mostaru, i javljam se putem otvorene, spontane aplikacije za junior developersku poziciju.\n\nPort8 je mostarski PropTech startup, većinski u vlasništvu švicarske kompanije emonitor AG, koji digitalizuje tržište nekretnina. Na vašem sajtu sam vidio da izričito pozivate na spontane prijave i onda kad nemate otvorenu poziciju, pa se javljam za junior developersku poziciju u nadi da biste imali mjesta za nekoga ko želi rasti zajedno sa malim, brzorastućim timom...\n\nS poštovanjem,\nNedim Jugo"
        );
        await AddInteraction(
            appPort8,
            ctPort8Careers,
            InteractionDirection.Inbound,
            InteractionChannel.Email,
            new DateTime(2026, 10, 1, 15, 43, 0, DateTimeKind.Utc),
            "Odg: port8.ba careers application from Nedim Jugo",
            "Poštovana Nedim,\n\nhvala Vam na prijavi i interesu za posao u Port8. Nažalost, trenutno nemamo otvorenu poziciju koja bi odgovarala Vašim kompetencijama.\n\nZadržat ćemo Vaš CV u našoj arhivi kako bismo Vas kontaktirali ako se otvori mjesto za Vas u budućnosti.\n\nJoš jednom se zahvaljujemo na prijavi i želimo Vam puno sreće i uspjeha u životu i radu.\n\nSrdačan pozdrav!"
        );

        // ==========================================
        // 14. NLB BANKA (f14)
        // ==========================================
        var cNlb = await GetOrCreateCompany("NLB Banka", "https://www.nlb.ba", "BiH", "Banking & Finance");
        await GetOrCreateApplication(
            cNlb,
            "IT pozicija / Specijalist u IT (forma)",
            ApplicationStatus.Ghosted,
            new DateTime(2026, 9, 6, 12, 0, 0, DateTimeKind.Utc),
            ApplicationSource.CompanyWebsite,
            "forma na stranici",
            WorkMode.Hybrid,
            "NLB Banka prije 25 dana poslao podatke putem forme, ništa nisam dobio odgovor.\nIzvor: pokusaji.txt:933",
            "Nikakav odgovor više od 25 dana nakon slanja forme."
        );

        // ==========================================
        // 15. ASA BANKA (f15)
        // ==========================================
        var cAsa = await GetOrCreateCompany("ASA Banka", "https://www.asabanka.ba", "BiH", "Banking & Finance");
        await GetOrCreateApplication(
            cAsa,
            "IT pozicija / IT sektor (forma)",
            ApplicationStatus.Ghosted,
            new DateTime(2026, 9, 6, 12, 15, 0, DateTimeKind.Utc),
            ApplicationSource.CompanyWebsite,
            "forma na stranici",
            WorkMode.Hybrid,
            "ASA Banka prije 25 dana poslao podatke putem forme, ništa nisam dobio odgovor.\nIzvor: pokusaji.txt:937",
            "Nikakav odgovor više od 25 dana nakon slanja forme."
        );

        // ==========================================
        // 16. ZIRAAT BANKA (f16)
        // ==========================================
        var cZiraat = await GetOrCreateCompany("Ziraat Banka", "https://www.ziraatbank.ba", "BiH", "Banking & Finance");
        await GetOrCreateApplication(
            cZiraat,
            "IT pozicija / Razvojni tim (forma)",
            ApplicationStatus.Ghosted,
            new DateTime(2026, 9, 6, 12, 30, 0, DateTimeKind.Utc),
            ApplicationSource.CompanyWebsite,
            "forma na stranici",
            WorkMode.Hybrid,
            "Ziraat Banka prije 25 dana poslao podatke putem forme, ništa nisam dobio odgovor.\nIzvor: pokusaji.txt:941",
            "Nikakav odgovor više od 25 dana nakon slanja forme."
        );

        // ==========================================
        // 17. SALT SQUARE / PERSONIFY HEALTH (f17)
        // ==========================================
        var cSaltSquare = await GetOrCreateCompany("Salt Square (Personify Health)", "https://saltsquare.io", "Sarajevo / Remote, BiH", "Software Development");
        var ctMirza = await GetOrCreateContact("Mirza", cSaltSquare.Id, "Team Lead / Contact", "mirza@saltsquare.io", null, null, ContactType.HiringManager);
        var ctDijana = await GetOrCreateContact("Dijana", cSaltSquare.Id, "Preporučilac", null, null, null, ContactType.Referrer, "Razgovarala s Mirzom o Nedimu");
        var appSaltSquare = await GetOrCreateApplication(
            cSaltSquare,
            "Junior Developer / Junior Project Manager",
            ApplicationStatus.Ghosted,
            new DateTime(2026, 9, 13, 13, 58, 0, DateTimeKind.Utc),
            ApplicationSource.Referral,
            "email: mirza@saltsquare.io (preko preporuke Dijane)",
            WorkMode.Remote,
            "Slao Mirzi mejl i preporuku CV za Personify Health, nema odgovora.\nIzvor: pokusaji.txt:950",
            "Bez odgovora više od 18 dana nakon poslanog emaila i preporuke."
        );
        await LinkApplicationContact(appSaltSquare, ctMirza);
        await LinkApplicationContact(appSaltSquare, ctDijana);
        await AddInteraction(
            appSaltSquare,
            ctMirza,
            InteractionDirection.Outbound,
            InteractionChannel.Email,
            new DateTime(2026, 9, 13, 13, 58, 0, DateTimeKind.Utc),
            "Prijava i preporuka - Personify Health pozicija",
            "Pozdrav Mirza,\n\nMoje ime je Nedim Jugo i javljam se povodom razgovora koji si imao sa Dijanom u vezi mene i potencijalnim otvorenim pozicijama u Vašem timu.\n\nZavršio sam Fakultet informacijskih tehnologija (FIT) u Mostaru i stekao titulu bachelora softverskog inženjeringa (240 ECTS). Tokom studija i kroz samostalne projekte fokusirao sam se na full-stack razvoj (primarno C#, ASP.NET Core, Angular, SQL Server, kao i React / Flutter), uz rad na AI i embedded sistemima. Pored čistog inženjerskog dijela, kroz dugogodišnje vođenje i organizaciju timskih projekata, edukativnih aktivnosti i događaja stekao sam snažan osjećaj za planiranje, praćenje rokova i koordinaciju, zbog čega sam podjednako otvoren i motivisan i za Junior Developer i za Junior Project Manager poziciju, naravno sve ovisno o tome šta je trenutno dostupno otvoren sam i za prijedloge.\n\nU prilogu ti šaljem svoj CV i motivaciono pismo, pripremljene na bosanskom i engleskom jeziku.\n\nStojim na raspolaganju za sastanak i razgovor u terminu kada Vama odgovara kako bi me tom prilikom upoznali i vidjeli kako bih se najbolje mogao uklopiti u Vaše trenutne projekte i potrebe tima.\n\nHvala ti na izdvojenom vremenu.\n\nSrdačan pozdrav,\n\nNedim Jugo\n0603185869\nnedim.jugoo@gmail.com"
        );

        // ==========================================
        // 18. INFOBIP (f18)
        // ==========================================
        var cInfobip = await GetOrCreateCompany("Infobip", "https://www.infobip.com", "Sarajevo / Tuzla / Remote", "Communications Platform as a Service");
        var ctKanita = await GetOrCreateContact("Kanita", cInfobip.Id, "Kolegica iz Infobipa", null, null, null, ContactType.Peer, "Savjetovala slanje na talent.acquisition@infobip.com");
        var appInfobip = await GetOrCreateApplication(
            cInfobip,
            "Junior software dev / Junior project manager (hibridna uloga)",
            ApplicationStatus.Ghosted,
            new DateTime(2026, 9, 15, 15, 55, 0, DateTimeKind.Utc),
            ApplicationSource.Other,
            "email: talent.acquisition@infobip.com",
            WorkMode.Hybrid,
            "Slao mejl nema odgovora. Prethodno je Kanita odgovorila na pitanja i predložila slanje direktno na ovaj mejl.\nIzvor: pokusaji.txt:980",
            "Bez odgovora više od 16 dana nakon direktnog slanja CV-ja i portfolija."
        );
        await LinkApplicationContact(appInfobip, ctKanita);
        await AddInteraction(
            appInfobip,
            null,
            InteractionDirection.Outbound,
            InteractionChannel.Email,
            new DateTime(2026, 9, 15, 15, 55, 0, DateTimeKind.Utc),
            "Prijava i portfolio - Junior Developer / PM uloga",
            "Poštovani,\n\nZovem se Nedim Jugo, bachelor softverskog inženjeringa iz Mostara, diplomirao sam na Univerzitetu „Džemal Bijedić“. Danas sam samoinicijativno kontaktirao Vaši kolegicu Kanitu, koja mi je odgovorila na neka pitanja ali i predložila da Vam se javim direktno na ovaj mejl i pošaljem svoj portfolio, pa se ovim putem javljam i raspitujem o mogućnosti prijave.\n\nDa vam ukratko kažem nešto o sebi - iza sebe imam tromjesečnu praksu u Garaža Makerspace-u, gdje sam bio dio tima koji je razvio zvaničnu, dvojezičnu web stranicu za Maker Faire Mostar (EU-finansirani festival sa korporativnim sponzorima), a pored uloge developera sam preuzeo i vodeću tehničku ulogu na tom projektu, kao i na Smart Storage-u, IoT sistemu koji spaja web aplikaciju, ESP32 mikrokontroler i prepoznavanje glasa.\n\nOsim toga, u posljednjih godinu dana sam samostalno izgradio i objavio nekoliko projekata u produkciji, svaki javno dostupan kao open-source i svaki u drugačijem domenu: GPS, Binny, EcoChallenge, Chord Hub...\n\nUnaprijed hvala izdvojenom vremenu,\nNedim Jugo\n+387 60 318 5869\nnedim.jugoo@gmail.com"
        );

        // ==========================================
        // 19. LIDL BIH (f19)
        // ==========================================
        var cLidl = await GetOrCreateCompany("Lidl BiH (IT odjel)", "https://karijera.lidl.ba", "Sarajevo / BiH", "Retail & Internal IT");
        var appLidl = await GetOrCreateApplication(
            cLidl,
            "Junior developer, IT odjel",
            ApplicationStatus.Ghosted,
            new DateTime(2026, 9, 15, 16, 27, 0, DateTimeKind.Utc),
            ApplicationSource.Other,
            "email: posao@lidl.ba",
            WorkMode.Onsite,
            "Slao mejl, nema odgovora.\nIzvor: pokusaji.txt:1018",
            "Bez odgovora 16 dana nakon slanja prijave."
        );
        await AddInteraction(
            appLidl,
            null,
            InteractionDirection.Outbound,
            InteractionChannel.Email,
            new DateTime(2026, 9, 15, 16, 27, 0, DateTimeKind.Utc),
            "Prijava za poziciju u IT odjelu – Nedim Jugo",
            "Poštovani Lidl tim,\n\nZovem se Nedim Jugo, diplomirani inženjer softvera (Univerzitet „Džemal Bijedić“, Mostar). Javljam se u vezi mogućnosti zaposlenja u IT odjelu Lidl BiH, ukoliko trenutno postoji potreba za nekim na toj poziciji.\n\nSvjestan sam da Lidl prije svega posluje u maloprodaji, ali mi je poznato da savremeni trgovački lanci danas imaju ozbiljne interne IT timove koji drže digitalizaciju poslovanja - od kasa i logistike do internih sistema - pa mi se čini da bi to mogla biti dobra prilika za mene kao junior developera.\n\nU prilogu vam šaljem CV i motivaciono pismo sa detaljnijim pregledom dosadašnjeg iskustva i projekata. Rado bih razgovarao o tome da li trenutno postoji prostor za mene u vašem timu.\n\nUnaprijed zahvaljujem na vremenu i odgovoru.\n\nS poštovanjem,\nNedim Jugo\n+387 60 318 5869\nnedim.jugoo@gmail.com"
        );

        // ==========================================
        // 20. MINISTRY OF PROGRAMMING (f20)
        // ==========================================
        var cMop = await GetOrCreateCompany("Ministry of Programming (MoP)", "https://ministryofprogramming.com", "Sarajevo / Full Remote", "Venture Builder & Software Development");
        var ctResadZacina = await GetOrCreateContact("Resad Zacina", cMop.Id, "Co-founder & CGO", "resad@ministryofprogramming.com", null, "https://www.linkedin.com/in/resad-zacina", ContactType.HiringManager, "Inicijalni kontakt na LinkedInu, uvezao s menadžmentom");
        var ctLejlaImamovic = await GetOrCreateContact("Lejla Imamovic", cMop.Id, "People Operations", "lejla.imamovic@ministryofprogramming.com", null, null, ContactType.Recruiter);
        var ctHajraSaletovic = await GetOrCreateContact("Hajra Saletovic", cMop.Id, "Talent Acquisition", "hajra.saletovic@ministryofprogramming.com", null, null, ContactType.Recruiter, "Obavila telefonski screening poziv");

        var appMop = await GetOrCreateApplication(
            cMop,
            "Junior Software Developer / Junior Project Manager (remote)",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 12, 12, 56, 0, DateTimeKind.Utc),
            ApplicationSource.Referral,
            "LinkedIn (Rešad Zacina) + email + telefon",
            WorkMode.Remote,
            "Rešad Začina CGO MoP javio mu se preko LinkedIna, uvezano preko mejla sa Lejlom i Hajrom. Hajra me jučer zvala na telefon i pričali jedno 7 min, rekla da sad nemaju ništa za mene.\nIzvor: pokusaji.txt:1048-1202",
            "Došao do faze telefonskog screening razgovora. Trenutno nemaju otvorenih junior rola na projektima."
        );
        await LinkApplicationContact(appMop, ctResadZacina);
        await LinkApplicationContact(appMop, ctLejlaImamovic);
        await LinkApplicationContact(appMop, ctHajraSaletovic);

        await AddInteraction(appMop, ctResadZacina, InteractionDirection.Outbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 12, 12, 56, 0, DateTimeKind.Utc), "Prijava u vezi moguće pozicije - Rešad Začina", "Poštovanje Rešade,\n\nVidio sam Vaš post gdje ste pozvali zainteresirane da se jave u DM, pa se javljam vezano za junior dev poziciju u Ministry of Programming. Znam da je subota i da vjerovatno nije idealno vrijeme za ovakvu poruku, ali sam odlučio da ne čekam i da Vam se javim odmah, kako ne bi propustio priliku.🙂\n\nŽivim u Mostaru, i s obzirom da ovdje trenutno nemate ured, htio bih pitati postoji li kod vas mogućnost remote ili hybrid angažmana za nekoga na junior poziciji.\n\nIza sebe imam nekoliko full-stack projekata koje sam sam vodio od ideje do produkcije, radim uglavnom sa ASP.NET Core, Angular, React i SQL Server bazama. Ali i pored svega ovoga imam završen Bachelor softverskog inženjeringa.\n\nBilo bi mi drago ako biste mogli pogledati moj profil i javiti mi ima li trenutno prostora za nekoga s mojim profilom. Ono što mogu garantovati je puno prilagođavanje Vašim standardima, ali i vidim tu priliku za dalje učenje i napredovanje. Otvoren sam za razgovor kad god Vama odgovara.\n\nHvala Vam puno i lijep pozdrav,\nNedim");
        await AddInteraction(appMop, ctResadZacina, InteractionDirection.Inbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 18, 12, 10, 0, DateTimeKind.Utc), "Rešad nudi uvezivanje sa menadžmentom", "cao Nedime\n\nsvakako cu te uvezati sa nasim menadzmentom da te imaju u vidu\n\nimamo uvijek prostora za dobre ljude\n\na svi su full remote tako da ne brini ;)\n\nposalji mi samo full cv pls");
        await AddInteraction(appMop, ctResadZacina, InteractionDirection.Outbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 18, 12, 24, 0, DateTimeKind.Utc), "Slanje CV i motivacionog pisma Rešadu", "Poštovanje ponovo Rešade jako mi je drago što mi želite pomoći i što ste odgovorili na poruku a ja Vama šaljem dva dokumenta CV i motivaciono koji će nadam se biti dovoljni Vašim timovima da me razmotre za razgovor kako bi vidjeli moje vještine.");
        await AddInteraction(appMop, ctResadZacina, InteractionDirection.Inbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 18, 12, 28, 0, DateTimeKind.Utc), "Rešad potvrda", "nema na cemu!");
        await AddInteraction(appMop, ctResadZacina, InteractionDirection.Inbound, InteractionChannel.Email, new DateTime(2026, 9, 18, 12, 46, 0, DateTimeKind.Utc), "Rešad Začina uvezuje sa Lejlom i Hajrom", "Hi Lejla & Hajra,\n\nConnecting you with Nedim Jugo. He indicated interest to work with us at MoP and hear more about the opportunities.\n\nThanks,\nResad");
        await AddInteraction(appMop, ctLejlaImamovic, InteractionDirection.Outbound, InteractionChannel.Email, new DateTime(2026, 9, 18, 14, 40, 0, DateTimeKind.Utc), "Email Lejli i Hajri sa CV-jem i motivacionim pismom", "Poštovane Lejla i Hajra,\n\nRešade, hvala puno na uvezivanju!\n\nDrago mi je što imamo priliku stupiti u kontakt. Kao što je Rešad spomenuo, izuzetno sam zainteresovan za mogućnosti angažmana u Ministry of Programmingu, posebno na poziciji Junior Software Developera ili Junior Projekt Menadžera.\n\nUkratko o meni: diplomirani sam inženjer softverskog inženjeringa, sa praktičnim iskustvom u izradi full-stack aplikacija, od ideje do produkcije, prvenstveno koristeći ASP.NET Core, React, Angular i SQL Server. Motivisan sam praktičnim rješavanjem problema, kontinuiranim učenjem i radom u okruženju sa visokim inženjerskim standardima.\n\nU prilogu vam dostavljam svoj CV i motivaciono pismo na engleskom i bosanskom.\n\nBilo bi mi drago da se čujemo putem kratkog uvodnog razgovora u terminu koji vama najviše odgovara.\n\nLijep pozdrav,\nNedim Jugo\nEmail: nedim.jugoo@gmail.com\nTel: +387 60 318 5869");
        await AddInteraction(appMop, ctResadZacina, InteractionDirection.Outbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 28, 13, 32, 0, DateTimeKind.Utc), "Praćenje statusa kod Rešada", "Pozdrav, ja se ponovo javljam samo kako bi provjerio situaciju oko pozicija i mogućnost u vašim timovima, pošto ste mi se Vi jedini odazvali i odgovorili. Ja bi samo volio znati da li mogu dobiti bilo kakvu informaciju od Vaših kolegica ili od vas u vezi pozicija, posto od onog dana kada ste nas Vi povezali prije 10 dana nisam dobio nikakvu povratnu informaciju. Pa bi volio čisto da znam otprilike na čemu sam, ako to nije problem, ili ostaje da još moram čekati odgovor.");
        await AddInteraction(appMop, ctResadZacina, InteractionDirection.Inbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 28, 14, 39, 0, DateTimeKind.Utc), "Rešad pinga kolegice", "hvala na javljanju\n\nmislim da su imale dosta posla i kandidata\n\npingam ih svakako");
        await AddInteraction(appMop, ctResadZacina, InteractionDirection.Outbound, InteractionChannel.LinkedIn, new DateTime(2026, 9, 28, 15, 20, 0, DateTimeKind.Utc), "Zahvala Rešadu", "Hvala Vama na pomoći i stvarno brzim odgovorima. Vjerujem da kolegice imaju dosta posla u rukama, meni je bilo bitno samo da saznam da ću dobiti odgovor, a sad vjerujem da hoću u narednim danima");
        await AddInteraction(appMop, ctHajraSaletovic, InteractionDirection.Inbound, InteractionChannel.Phone, new DateTime(2026, 9, 30, 11, 0, 0, DateTimeKind.Utc), "Telefonski screening razgovor (~7 min) sa Hajrom", "onda me hajra jucer zvala na telefon i pricali jedno 7min i rekla da sad nemaju nista za mene");

        // Add Screening Interview record for MoP
        var interviewExists = await db.Interviews.IgnoreQueryFilters().AnyAsync(i => i.ApplicationId == appMop.Id, ct);
        if (!interviewExists)
        {
            var mopInterview = new Interview
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ApplicationId = appMop.Id,
                Type = InterviewType.HR,
                Format = InterviewFormat.Phone,
                Status = InterviewStatus.Completed,
                ScheduledAt = new DateTime(2026, 9, 30, 11, 0, 0, DateTimeKind.Utc),
                DurationMinutes = 10,
                PrepNotes = "Telefonski screening razgovor (~7 min) sa Hajrom Saletović (MoP). Predstavljanje kvalifikacija i razgovor o mogućim ulogama.",
                OutcomeNotes = "Ishod: trenutno nemaju otvorenih junior pozicija.",
                CreatedAt = new DateTime(2026, 9, 30, 11, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 9, 30, 11, 10, 0, DateTimeKind.Utc)
            };
            db.Interviews.Add(mopInterview);
        }

        // ==========================================
        // 21. XSOFT (f21)
        // ==========================================
        var cXsoft = await GetOrCreateCompany("XSoft", "https://xsoft.ba", "Mostar, BiH", "Software Solutions");
        var ctMaja = await GetOrCreateContact("Maja", cXsoft.Id, "Kontakt u firmi", null, null, null, ContactType.Referrer, "Proslijedila CV Danijelu");
        var ctDanijel = await GetOrCreateContact("Danijel", cXsoft.Id, "Menadžer / Donosilac odluka", null, null, null, ContactType.HiringManager);
        var appXsoft = await GetOrCreateApplication(
            cXsoft,
            "Junior Developer (otvorena prijava)",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 19, 10, 0, 0, DateTimeKind.Utc),
            ApplicationSource.Referral,
            "CV i motivaciono poslano Maji",
            WorkMode.Onsite,
            "XSoft slao Maji CV i motivaciono prije 12 dana ona poslala Danijelu on prije 4 dana odgovorio da nema ništa sad za mene.\nIzvor: pokusaji.txt:1206",
            "Danijel izričito odgovorio da nema otvorenih pozicija."
        );
        await LinkApplicationContact(appXsoft, ctMaja);
        await LinkApplicationContact(appXsoft, ctDanijel);

        // ==========================================
        // 22. CONFIGPOS (f22)
        // ==========================================
        var cConfigPos = await GetOrCreateCompany("ConfigPOS", "https://configpos.ba", "Mostar, BiH", "POS & Retail Software");
        await GetOrCreateApplication(
            cConfigPos,
            "Junior Developer / Tehnička podrška",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 27, 10, 0, 0, DateTimeKind.Utc),
            ApplicationSource.Other,
            "telefon",
            WorkMode.Onsite,
            "ConfigPOS zvao prije 4 dana nema nikakvih pozicija.\nIzvor: pokusaji.txt:1210",
            "Telefonski razgovor: potvrđeno da nema nikakvih otvorenih pozicija."
        );

        // ==========================================
        // 23. BLOOMTEQ (f23)
        // ==========================================
        var cBloomteq = await GetOrCreateCompany("Bloomteq", "https://bloomteq.com", "Sarajevo / Remote", "Software & AI Solutions");
        var ctElmirBabovic = await GetOrCreateContact("Elmir Babović", cBloomteq.Id, "Poznanik / Inženjer", null, null, null, ContactType.Peer, "Poznanik u firmi");
        var appBloomteq = await GetOrCreateApplication(
            cBloomteq,
            "Junior Developer / Tehnički profil",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 20, 15, 0, 0, DateTimeKind.Utc),
            ApplicationSource.Referral,
            "poruka poznaniku (Elmir Babović)",
            WorkMode.Remote,
            "Bloomteq pisao Elmiru Baboviću i ništa nema sad. Korisnik poručio da ih neće dalje zamarati.\nIzvor: pokusaji.txt:1214",
            "Nema pozicije sada."
        );
        await LinkApplicationContact(appBloomteq, ctElmirBabovic);
        await AddInteraction(
            appBloomteq,
            ctElmirBabovic,
            InteractionDirection.Outbound,
            InteractionChannel.Message,
            new DateTime(2026, 9, 20, 15, 0, 0, DateTimeKind.Utc),
            "Poruka Elmiru Baboviću sa CV-jem",
            "Nisam Vas direktno tražio, ali kada sam Vas ipak vidio, nisam Vas htio puno ometati jer ste bili na ispitu. Htio bi Vam reci da ako bio bih Vam veoma zahvalan ako biste me imali na umu ili eventualno preporučili ukoliko se pojavi neka prilika u IT-ju.\n\nKao što znate, ja sam iz Mostara, pa bi mi zbog toga najviše odgovarala neka firma u Mostaru ili ako je opcija van Mostara da omogućava hybrid ili remote rad. Naravno, otvoren sam i za druge opcije, ali bi mi trenutno preseljenje u Sarajevo zbog troškova života bilo dosta teško izvodljivo.\n\nPoslat ću Vam ovdje i svoj CV, pa ga možete pogledati kada budete imali vremena, čisto da imate bolji uvid u moje dosadašnje iskustvo, projekte i rad.\n\nHvala Vam unaprijed na vremenu i pomoći, zaista bih bio zahvalan na komunikaciji i prijašnjem savijetu, a ja Vas neću više ovdje zamarati i pisati."
        );
        await AddInteraction(
            appBloomteq,
            ctElmirBabovic,
            InteractionDirection.Inbound,
            InteractionChannel.Message,
            new DateTime(2026, 9, 20, 16, 0, 0, DateTimeKind.Utc),
            "Odgovor Elmir Babović (reakcija)",
            "BABOVIC\n👍"
        );

        // ==========================================
        // 24. EVONA (f24)
        // ==========================================
        var cEvona = await GetOrCreateCompany("Evona", "https://evona-electronic.com", "Mostar, BiH", "Betting & Gaming Systems");
        var ctMarija = await GetOrCreateContact("Marija", cEvona.Id, "Kontakt u firmi", null, null, null, ContactType.Peer);
        var ctIvan = await GetOrCreateContact("Ivan", cEvona.Id, "Kontakt u firmi", null, null, null, ContactType.Peer);
        var appEvona = await GetOrCreateApplication(
            cEvona,
            "Junior Developer (otvorena prijava)",
            ApplicationStatus.Rejected,
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            ApplicationSource.Other,
            "poruke Mariji i Ivanu",
            WorkMode.Onsite,
            "Pisao Mariji i Ivanu prije 30 dana i oboje rekli da nema pozicija.\nIzvor: pokusaji.txt:1233",
            "Oboje potvrdili da nema otvorenih pozicija."
        );
        await LinkApplicationContact(appEvona, ctMarija);
        await LinkApplicationContact(appEvona, ctIvan);

        // ==========================================
        // 25. EXTERNAL REFERRER: EDIN SALIHAGIĆ
        // ==========================================
        var ctEdinSalihagic = await GetOrCreateContact(
            "Edin Salihagić",
            null,
            "Preporučilac",
            "edin.salihagic@gmail.com",
            null,
            null,
            ContactType.Referrer,
            "Pisao edin.salihagic@gmail.com za preporuku nekim firmama, on nekome slao ne znam kome.\nIzvor: pokusaji.txt:60-62"
        );
        await AddInteraction(
            null,
            ctEdinSalihagic,
            InteractionDirection.Outbound,
            InteractionChannel.Email,
            new DateTime(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc),
            "Slanje linkova na CV, LinkedIn i GitHub za preporuke",
            "Ispod imaju moja dva CV-ja javno dostupna i na mom LinkedIn-u.\n\nLink na CV:\nhttps://drive.google.com/drive/folders/13r56lFxyhHDBgArXek21Qprh_8Gtb-QQ?usp=sharing\n\nLink na LinkedIn:\nhttps://www.linkedin.com/in/nedim-jugo-492b99277?utm_source=share_via&utm_content=profile&utm_medium=member_android\n\nLink na GitHub:\nhttps://github.com/NedimJugo"
        );

        // ==========================================
        // 26. FOLLOW-UP TASKS FOR ACTIVE PIPELINE
        // ==========================================
        var existingTasks = await db.Tasks.IgnoreQueryFilters().Where(t => t.UserId == userId).ToListAsync(ct);
        if (!existingTasks.Any())
        {
            db.Tasks.AddRange(new[]
            {
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ApplicationId = appZira.Id,
                    Title = "Prijaviti se na Dev ZTA program čim postane aktivan",
                    Notes = "Armin Babović je preporučio da se prijava za Dev ZTA dobro spremi, da se javi Medini iz tima i pošalju relevantni full-stack projekti.",
                    DueAt = DateTime.UtcNow.AddDays(7),
                    Source = TaskSource.Manual,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ApplicationId = appHtec.Id,
                    Title = "Pratiti HTEC web stranicu i javiti se Damiru Avdiću",
                    Notes = "Ukoliko se otvori junior software developer ili junior PM rola, javiti se Damiru Avdiću koji se ponudio da proslijedi CV i motivaciono prema TA timu.",
                    DueAt = DateTime.UtcNow.AddDays(14),
                    Source = TaskSource.Manual,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ApplicationId = appBhTelecom.Id,
                    Title = "Provjeriti objavu novih konkursa na bhtelecom.ba/karijera",
                    Notes = "Pratiti javne konkurse za IT sektor i eventualne pozicije na određeno vrijeme do 6 mjeseci.",
                    DueAt = DateTime.UtcNow.AddDays(21),
                    Source = TaskSource.Manual,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ApplicationId = appSaasSolutions.Id,
                    Title = "Periodični follow-up sa Matejom Šumić (SaaS Solutions)",
                    Notes = "CV je sačuvan u njihovoj bazi, provjeriti stanje otvorenih pozicija za mostarski ured.",
                    DueAt = DateTime.UtcNow.AddDays(30),
                    Source = TaskSource.Manual,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            });
        }

        await db.SaveChangesAsync(ct);
        return user;
    }
}
