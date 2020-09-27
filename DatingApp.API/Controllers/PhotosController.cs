using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Helpers;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
    [Authorize]
    [Route("api/users/{userId}/photos")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _cloudinary;
        public PhotosController(IDatingRepository repo, IMapper mapper, IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _cloudinaryConfig = cloudinaryConfig;
            _mapper = mapper;
            _repo = repo;
            Account acc = new Account(
                 _cloudinaryConfig.Value.CloudName,
                 _cloudinaryConfig.Value.ApiKey,
                 _cloudinaryConfig.Value.ApiSecret
            );
            _cloudinary = new Cloudinary(acc);
        }


        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var photoFromRepo = await _repo.GetPhoto(id);
            var photo = _mapper.Map<PhotoforReturnDto>(photoFromRepo);
            return Ok(photo);
        }



        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId, [FromForm]PhotoForCreationDto photoforCreationDto)
        {
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)){
                return Unauthorized();
            }

            var userFromRepo = await _repo.GetUser(userId);

            var file = photoforCreationDto.File;

            var uploadResult = new ImageUploadResult();

            //to upload this file in cloudinary 
            if(file.Length > 0)
            {   
                 using (var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.Name,stream),
                        Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
                    }; 
                    uploadResult = _cloudinary.Upload(uploadParams);
                }
            }
        
            photoforCreationDto.Url = uploadResult.Url.ToString();
            photoforCreationDto.PublicId = uploadResult.PublicId;
            var photo = _mapper.Map<Photo>(photoforCreationDto);
            if(!userFromRepo.Photos.Any(u=>u.IsMain)){photo.IsMain=true;}

            userFromRepo.Photos.Add(photo);
            if(await _repo.SaveAll())
            {
                var photoforReturn = _mapper.Map<PhotoforReturnDto>(photo);
                return CreatedAtRoute("GetPhoto", new {userId = userId , id = photo.Id}, photoforReturn);
            }

            return BadRequest("Could not upload photo");
        }



        [HttpPost("{id}/setMain")]
        public async Task<IActionResult> setMainPhoto(int userId, int id)
        {
            //authorization
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)){
                return Unauthorized();
            }

            //checking if the user trying to set the photo is among from his own photo.
            var userFromRepo = await _repo.GetUser(userId);
            if(!userFromRepo.Photos.Any(p=>p.Id ==id)){
                return BadRequest("Photo could not found");
            }

            //checking if the photo is already a main photo.
            var photoFromRepo = await _repo.GetPhoto(id);
            if(photoFromRepo.IsMain){ return BadRequest("This is already the profile picture");}

            var mainPhoto = await _repo.GetMainPhoto(userId);
            //System.Console.WriteLine(mainPhoto.IsMain);
            mainPhoto.IsMain = false;
            photoFromRepo.IsMain = true;

            if(await _repo.SaveAll()){return NoContent();}
            return BadRequest("Could not change the profile picture");
            
        }



        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhotoForUser(int userId, int id)
        {
            //authorization
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)){
                return Unauthorized();
            }

            //checking if the user trying to set the photo is among from his own photo.
            var userFromRepo = await _repo.GetUser(userId);
            if(!userFromRepo.Photos.Any(p=>p.Id ==id)){
                return BadRequest("Photo could not found");
            }

            //checking if the photo is already a main photo.
            var photoFromRepo = await _repo.GetPhoto(id);
            if(photoFromRepo.IsMain){ return BadRequest("Cannot delete main photo");}
            if(photoFromRepo.PublicId!=null)
            {
            var deleteParams = new DeletionParams(photoFromRepo.PublicId);
            var result = _cloudinary.Destroy(deleteParams);
                if(result.Result=="ok")
                {
                    _repo.Delete(photoFromRepo);
                }
            }
            
            _repo.Delete(photoFromRepo);

            if(await _repo.SaveAll()){return Ok();}
            return BadRequest("could not delete photo");
        }
    }

}