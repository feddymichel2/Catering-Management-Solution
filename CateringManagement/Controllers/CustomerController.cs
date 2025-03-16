using CateringManagement.CustomControllers;
using CateringManagement.Data;
using CateringManagement.Models;
using CateringManagement.Utilities;
using CateringManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using String = System.String;

namespace CateringManagement.Controllers
{
    [Authorize]
    public class CustomerController : ElephantController
    {
        //for sending email
        private readonly IMyEmailSender _emailSender;
        private readonly CateringContext _context;

        public CustomerController(CateringContext context, IMyEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        // GET: Customers
        public async Task<IActionResult> Index(int? page, int? pageSizeID,
            string actionButton, string sortDirection = "asc", string sortField = "Customer")
        {
            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Customer", "Company Name", "Phone", "Customer Code" };

            var customers = _context.Customers
                .Include(p => p.CustomerThumbnail)
                .AsNoTracking();

            //Before we sort, see if we have called for a change of filtering or sorting
            if (!String.IsNullOrEmpty(actionButton)) //Form Submitted!
            {
                page = 1;//Reset page to start

                if (sortOptions.Contains(actionButton))//Change of sort is requested
                {
                    if (actionButton == sortField) //Reverse order on same field
                    {
                        sortDirection = sortDirection == "asc" ? "desc" : "asc";
                    }
                    sortField = actionButton;//Sort by the button clicked
                }
            }
            //Now we know which field and direction to sort by
            if (sortField == "Company Name")
            {
                if (sortDirection == "asc")
                {
                    customers = customers
                        .OrderBy(p => p.CompanyName);
                }
                else
                {
                    customers = customers
                        .OrderByDescending(p => p.CompanyName);
                }
            }
            else if (sortField == "Phone")
            {
                if (sortDirection == "asc")
                {
                    customers = customers
                        .OrderBy(p => p.Phone);
                }
                else
                {
                    customers = customers
                        .OrderByDescending(p => p.Phone);
                }
            }
            else if (sortField == "Customer Code")
            {
                if (sortDirection == "asc")
                {
                    customers = customers
                        .OrderBy(p => p.CustomerCode);
                }
                else
                {
                    customers = customers
                        .OrderByDescending(p => p.CustomerCode);
                }
            }
            else //Sorting by Customer
            {
                if (sortDirection == "asc")
                {
                    customers = customers
                        .OrderBy(p => p.LastName)
                        .ThenBy(p => p.FirstName);
                }
                else
                {
                    customers = customers
                        .OrderByDescending(p => p.LastName)
                        .ThenByDescending(p => p.FirstName);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, ControllerName());
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<Customer>.CreateAsync(customers.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Customers == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .Include(c => c.Functions)
                .Include(p => p.CustomerPhoto)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // GET: Customers/Create
        [Authorize(Roles = "Admin,Supervisor,Staff")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Supervisor,Staff")]
        public async Task<IActionResult> Create([Bind("ID,FirstName,MiddleName,LastName,CompanyName," +
            "Phone,CustomerCode")] Customer customer, IFormFile thePicture)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await AddPicture(customer, thePicture);
                    _context.Add(customer);
                    await _context.SaveChangesAsync();
                    //return RedirectToAction("Details", new { customer.ID });
                    //Send on to add Functions
                    return RedirectToAction("Index", "CustomerFunction", new { CustomerID = customer.ID });
                }
            }
            catch (DbUpdateException dex)
            {
                if (dex.GetBaseException().Message.Contains("UNIQUE constraint failed: Customers.CustomerCode"))
                {
                    ModelState.AddModelError("CustomerCode", "Unable to save changes. Remember, you cannot have duplicate Customer Codes.");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }

            return View(customer);
        }

        // GET: Customers/Edit/5
        [Authorize(Roles = "Admin,Supervisor,Staff")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Customers == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .Include(p => p.CustomerPhoto)
                .FirstOrDefaultAsync(p => p.ID == id);
            if (customer == null)
            {
                return NotFound();
            }
            //If in Staff role, check if they created the Customer
            if (User.IsInRole("Staff"))
            {
                if (customer.CreatedBy != User.Identity.Name)
                {
                    //They did NOT create the Customer
                    ModelState.AddModelError("", "You cannot edit " + customer.FullName +
                        " becuase you did not enter them into the system.");
                    ViewData["Disabled"] = "disabled=disabled";
                }
            }
            return View(customer);
        }

        // POST: Customers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Supervisor,Staff")]
        public async Task<IActionResult> Edit(int id, string chkRemoveImage, IFormFile thePicture)
        {
            //Go get the customer to update
            var customerToUpdate = await _context.Customers
                .Include(p => p.CustomerPhoto)
                .FirstOrDefaultAsync(p => p.ID == id);

            //Check that we got the customer or exit with a not found error
            if (customerToUpdate == null)
            {
                return NotFound();
            }
            //If in Staff role, check if they created the Customer
            if (User.IsInRole("Staff"))
            {
                if (customerToUpdate.CreatedBy != User.Identity.Name)
                {
                    //They did NOT create the Customer
                    ModelState.AddModelError("", "You cannot edit " + customerToUpdate.FullName +
                        " becuase you did not enter them into the system.");
                    ViewData["Disabled"] = "disabled=disabled";
                    return View(customerToUpdate);
                }
            }
            //Try updating it with the values posted
            if (await TryUpdateModelAsync<Customer>(customerToUpdate, "",
                c => c.FirstName, c => c.MiddleName, c => c.LastName, c => c.CompanyName,
                c => c.Phone, c => c.CustomerCode))
            {
                try
                {
                    //For the image
                    if (chkRemoveImage != null)
                    {
                        //If we are just deleting the two versions of the photo, we need to make sure the Change Tracker knows
                        //about them both so go get the Thumbnail since we did not include it.
                        customerToUpdate.CustomerThumbnail = _context.CustomerThumbnails.Where(p => p.CustomerID == customerToUpdate.ID).FirstOrDefault();
                        //Then, setting them to null will cause them to be deleted from the database.
                        customerToUpdate.CustomerPhoto = null;
                        customerToUpdate.CustomerThumbnail = null;
                    }
                    else
                    {
                        await AddPicture(customerToUpdate, thePicture);
                    }
                    await _context.SaveChangesAsync();
                    //Send on to Functions
                    return RedirectToAction("Index", "CustomerFunction", new { CustomerID = customerToUpdate.ID });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(customerToUpdate.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (DbUpdateException dex)
                {
                    if (dex.GetBaseException().Message.Contains("UNIQUE constraint failed: Customers.CustomerCode"))
                    {
                        ModelState.AddModelError("CustomerCode", "Unable to save changes. Remember, you cannot have duplicate Customer Codes.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                    }
                }
            }
            return View(customerToUpdate);
        }

        // GET: Customers/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Customers == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Customers == null)
            {
                return Problem("There are no Customers to delete.");
            }
            var customer = await _context.Customers.FindAsync(id);
            try
            {
                if (customer != null)
                {
                    _context.Customers.Remove(customer);
                }
                await _context.SaveChangesAsync();
                return Redirect(ViewData["returnURL"].ToString());
            }
            catch (DbUpdateException dex)
            {
                if (dex.GetBaseException().Message.Contains("FOREIGN KEY constraint failed"))
                {
                    ModelState.AddModelError("", "Unable to Delete Customer. Remember, you cannot delete a Customer that has a function in the system.");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }
            return View(customer);

        }

        // GET/POST: Customer/Notification/5
        public async Task<IActionResult> NotificationOne(int? CustomerID, string Subject, string emailContent)
        {

            ViewData["CustomerID"] = new SelectList(_context.Customers
                .Where(d => d.EMail != null)
                .OrderBy(d => d.LastName)
                .ThenBy(d => d.FirstName), "ID", "Summary");

            if(CustomerID != null)
            {
                if (string.IsNullOrEmpty(Subject) || string.IsNullOrEmpty(emailContent))
                {
                    ViewData["Message"] = "You must enter both a Subject and some message Content before sending the message.";
                }
                else
                {
                    Customer customer = _context.Customers.Where(c => c.ID == CustomerID).FirstOrDefault();
                    if (customer.EMail != User.Identity.Name)
                    {
                        ViewData["Message"] = "Sorry but you can only send an email to yourself in this demo.";
                    }
                    else
                    {
                        try
                        {
                            //Send a Message.
                            if (customer != null)
                            {
                                string Content = "<p>" + emailContent + "</p><p>Please access the <strong>Niagara College</strong> web site to review.</p>";
                                await _emailSender.SendOneAsync(customer.Summary, customer.EMail, Subject, Content);
                                ViewData["Message"] = "Message sent to Customer : " + customer.Summary;
                            }
                            else
                            {
                                ViewData["Message"] = "Message NOT sent!  No Customer.";
                            }
                        }
                        catch (Exception ex)
                        {
                            string errMsg = ex.GetBaseException().Message;
                            ViewData["Message"] = "Error: Could not send email message to " + customer.Summary;
                        }
                    }
                }
            }
            return View();
        }

        // GET/POST: Customer/Notification/5
        public async Task<IActionResult> NotificationMultiple(string[] selectedOptions, string Subject, string emailContent)
        {

            PopulateCustomerEmailData(selectedOptions);

            if (selectedOptions.Length > 0)
            {
                if (string.IsNullOrEmpty(Subject) || string.IsNullOrEmpty(emailContent))
                {
                    ViewData["Message"] = "You must enter both a Subject and some message Content before sending the message.";
                }
                else
                {
                    //Get the Customers
                    var selectedOptionsHS = new HashSet<string>(selectedOptions);
                    List<EmailAddress> folks = (from c in _context.Customers
                                                where selectedOptionsHS.Contains(c.ID.ToString())
                                                //This next line restricts it to just
                                                //the user signed in.  Comment this out to actually send to all selected
                                                && c.EMail == User.Identity.Name 
                                                select new EmailAddress
                                                {
                                                    Name = c.FullName,
                                                    Address = c.EMail
                                                }).ToList();
                    int folksCount = folks.Count;
                    int selectedCount = selectedOptionsHS.Count;
                    if (folksCount > 0)
                    {
                        try
                        {
                            var msg = new EmailMessage()
                            {
                                ToAddresses = folks,
                                Subject = Subject,
                                Content = "<p>" + emailContent + "</p><p>Please access the <strong>Niagara College</strong> web site to review.</p>"

                            };
                            await _emailSender.SendToManyAsync(msg);
                            ViewData["Message"] = "Message sent to " + folksCount + " Customer out of the " + selectedCount + " Customer"
                                + ((selectedCount == 1) ? " selected." : "s selected.");
                        }
                        catch (Exception ex)
                        {
                            string errMsg = ex.GetBaseException().Message;
                            ViewData["Message"] = "Error: Could not send email message to the selected customers.";
                        }
                        
                    }
                    else
                    {
                        ViewData["Message"] = "Sorry but you can only send an email to yourself in this demo and you were not selected.";
                    }
                }
            }
            return View();
        }

        private void PopulateCustomerEmailData(string[] selectedOptions)
        {
            
            var allOptions = _context.Customers
                .Where(d => d.EMail != null)
                .OrderBy(d => d.LastName)
                .ThenBy(d => d.FirstName);
            //var currentOptionsHS = new HashSet<int>(doctor.DoctorSpecialties.Select(b => b.SpecialtyID));
            var selectedOptionsHS = new HashSet<string>(selectedOptions);

            //Instead of one list with a boolean, we will make two lists
            var selected = new List<ListOptionVM>();
            var available = new List<ListOptionVM>();
            foreach (var c in allOptions)
            {
                if (selectedOptionsHS.Contains(c.ID.ToString()))
                {
                    selected.Add(new ListOptionVM
                    {
                        ID = c.ID,
                        DisplayText = c.EmailSummary
                    });
                }
                else
                {
                    available.Add(new ListOptionVM
                    {
                        ID = c.ID,
                        DisplayText = c.EmailSummary
                    });
                }
            }

            ViewData["selOpts"] = new MultiSelectList(selected.OrderBy(s => s.DisplayText), "ID", "DisplayText");
            ViewData["availOpts"] = new MultiSelectList(available.OrderBy(s => s.DisplayText), "ID", "DisplayText");
        }
        private async Task AddPicture(Customer customer, IFormFile thePicture)
        {
            //Get the picture and save it with the Customer (2 sizes)
            if (thePicture != null)
            {
                string mimeType = thePicture.ContentType;
                long fileLength = thePicture.Length;
                if (!(mimeType == "" || fileLength == 0))//Looks like we have a file!!!
                {
                    if (mimeType.Contains("image"))
                    {
                        using var memoryStream = new MemoryStream();
                        await thePicture.CopyToAsync(memoryStream);
                        var pictureArray = memoryStream.ToArray();//Gives us the Byte[]

                        //Check if we are replacing or creating new
                        if (customer.CustomerPhoto != null)
                        {
                            //We already have pictures so just replace the Byte[]
                            customer.CustomerPhoto.Content = ResizeImage.shrinkImageWebp(pictureArray, 500, 600);

                            //Get the Thumbnail so we can update it.  Remember we didn't include it
                            customer.CustomerThumbnail = _context.CustomerThumbnails.Where(p => p.CustomerID == customer.ID).FirstOrDefault();
                            customer.CustomerThumbnail.Content = ResizeImage.shrinkImageWebp(pictureArray, 75, 90);
                        }
                        else //No pictures saved so start new
                        {
                            customer.CustomerPhoto = new CustomerPhoto
                            {
                                Content = ResizeImage.shrinkImageWebp(pictureArray, 500, 600),
                                MimeType = "image/webp"
                            };
                            customer.CustomerThumbnail = new CustomerThumbnail
                            {
                                Content = ResizeImage.shrinkImageWebp(pictureArray, 75, 90),
                                MimeType = "image/webp"
                            };
                        }
                    }
                }
            }
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.ID == id);
        }
    }
}
